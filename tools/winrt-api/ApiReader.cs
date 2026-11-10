using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace WinRtApi;

/// <summary>A property: its name, its type as WinRT spells it, whether it can be set, and whether it's a dependency property.</summary>
internal sealed record ApiProperty(string Name, string Type, bool Settable, bool Dependency);

/// <summary>An event and its handler type.</summary>
internal sealed record ApiEvent(string Name, string Handler);

/// <summary>A method: its name, its signature, and whether it's an overridable <c>protected</c> one (<c>OnToggle</c>).</summary>
internal sealed record ApiMethod(string Name, string Signature, bool Overridable);

/// <summary>What one type in the chain declares itself.</summary>
internal sealed record ApiType(string FullName, IReadOnlyList<ApiProperty> Properties, IReadOnlyList<ApiEvent> Events, IReadOnlyList<ApiMethod> Methods)
{
    public string Name => FullName[(FullName.LastIndexOf('.') + 1)..];
}

/// <summary>
/// Reads a control's API out of a WinRT metadata file (<c>Windows.winmd</c>), as data only: the
/// members each type declares, from the control down its base types to, not including,
/// <c>stopAt</c>. A WinRT dependency property shows as a static <c>…Property</c> property, so it's
/// folded into the instance property it backs. WinRT metadata carries no template parts or visual
/// states: those come from <c>generic.xaml</c> (<see cref="TemplateReader"/>).
/// </summary>
internal sealed class ApiReader : IDisposable
{
    private readonly PEReader _pe;
    private readonly MetadataReader _md;
    private readonly Dictionary<string, TypeDefinitionHandle> _types = new(StringComparer.Ordinal);

    public ApiReader(string path)
    {
        _pe = new PEReader(File.OpenRead(path));
        _md = _pe.GetMetadataReader();
        foreach (var handle in _md.TypeDefinitions)
        {
            var type = _md.GetTypeDefinition(handle);
            _types[Qualified(type)] = handle;
        }
    }

    public void Dispose() => _pe.Dispose();

    /// <summary>Whether the metadata defines <paramref name="fullName"/>.</summary>
    public bool Contains(string fullName) => _types.ContainsKey(fullName);

    /// <summary>
    /// The chain from <paramref name="fullName"/> down its base types to, not including,
    /// <paramref name="stopAt"/>. The type itself is always in it, even when it is the stop.
    /// </summary>
    public IReadOnlyList<ApiType> Chain(string fullName, string stopAt)
    {
        if (!_types.ContainsKey(fullName))
        {
            throw new ArgumentException($"{fullName} isn't in the metadata.");
        }

        var chain = new List<ApiType>();
        for (var name = fullName; (name == fullName || name != stopAt) && _types.TryGetValue(name, out var handle);)
        {
            var type = _md.GetTypeDefinition(handle);
            chain.Add(Read(name, type));
            if (type.BaseType.IsNil)
            {
                break;
            }

            name = NameOf(type.BaseType);
        }

        return chain;
    }

    /// <summary>
    /// An enum's members and their values, in the metadata's order. <paramref name="fullName"/> is a
    /// full name, or a name under <c>Windows.UI.Xaml.Controls</c>.
    /// </summary>
    public IReadOnlyList<(string Name, long Value)> EnumMembers(string fullName)
    {
        if (!_types.TryGetValue(fullName, out var handle) && !_types.TryGetValue("Windows.UI.Xaml.Controls." + fullName, out handle))
        {
            throw new ArgumentException($"{fullName} isn't in the metadata.");
        }

        var type = _md.GetTypeDefinition(handle);
        if (type.BaseType.IsNil || NameOf(type.BaseType) != "System.Enum")
        {
            throw new ArgumentException($"{fullName} isn't an enum.");
        }

        var members = new List<(string, long)>();
        foreach (var fieldHandle in type.GetFields())
        {
            var field = _md.GetFieldDefinition(fieldHandle);
            if ((field.Attributes & FieldAttributes.Literal) == 0)
            {
                continue; // value__, the enum's storage
            }

            var blob = _md.GetBlobReader(_md.GetConstant(field.GetDefaultValue()).Value);
            var value = blob.Length switch
            {
                1 => blob.ReadSByte(),
                2 => blob.ReadInt16(),
                4 => blob.ReadInt32(),
                _ => blob.ReadInt64(),
            };
            members.Add((_md.GetString(field.Name), value));
        }

        return members;
    }

    private ApiType Read(string fullName, TypeDefinition type)
    {
        var provider = new TypeNames(this);
        var statics = new HashSet<string>(StringComparer.Ordinal);
        var instance = new List<(string Name, string Type, bool Settable)>();
        foreach (var handle in type.GetProperties())
        {
            var property = _md.GetPropertyDefinition(handle);
            var accessors = property.GetAccessors();
            var getter = accessors.Getter.IsNil ? (MethodDefinition?)null : _md.GetMethodDefinition(accessors.Getter);
            if (getter is not { } get || !IsVisible(get.Attributes))
            {
                continue;
            }

            var name = _md.GetString(property.Name);
            if ((get.Attributes & MethodAttributes.Static) != 0)
            {
                statics.Add(name);
                continue;
            }

            var signature = property.DecodeSignature(provider, genericContext: null);
            instance.Add((name, signature.ReturnType, !accessors.Setter.IsNil));
        }

        var properties = instance
            .Select(p => new ApiProperty(p.Name, p.Type, p.Settable, statics.Contains(p.Name + "Property")))
            .ToList();

        var events = type.GetEvents()
            .Select(_md.GetEventDefinition)
            .Where(e => !e.GetAccessors().Adder.IsNil && IsVisible(_md.GetMethodDefinition(e.GetAccessors().Adder).Attributes))
            .Select(e => new ApiEvent(_md.GetString(e.Name), Short(NameOf(e.Type))))
            .ToList();

        var methods = new List<ApiMethod>();
        foreach (var handle in type.GetMethods())
        {
            var method = _md.GetMethodDefinition(handle);
            var name = _md.GetString(method.Name);
            var attributes = method.Attributes;
            if ((attributes & MethodAttributes.SpecialName) != 0 || (attributes & MethodAttributes.Static) != 0 || !IsVisible(attributes))
            {
                continue;
            }

            var signature = method.DecodeSignature(provider, genericContext: null);
            var parameters = method.GetParameters()
                .Select(_md.GetParameter)
                .Where(p => p.SequenceNumber > 0)
                .OrderBy(p => p.SequenceNumber)
                .Select(p => _md.GetString(p.Name))
                .ToList();
            var arguments = string.Join(", ", signature.ParameterTypes.Select((t, i) => i < parameters.Count ? $"{t} {parameters[i]}" : t));
            // Every protected instance method in a .winmd is virtual; one that can't be overridden is also final.
            var overridable = (attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family
                && (attributes & MethodAttributes.Virtual) != 0
                && (attributes & MethodAttributes.Final) == 0;
            methods.Add(new ApiMethod(name, $"{signature.ReturnType} {name}({arguments})", overridable));
        }

        return new ApiType(fullName, properties, events, methods);
    }

    private static bool IsVisible(MethodAttributes attributes) =>
        (attributes & MethodAttributes.MemberAccessMask) is MethodAttributes.Public or MethodAttributes.Family or MethodAttributes.FamORAssem;

    private string Qualified(TypeDefinition type)
    {
        var name = _md.GetString(type.Name);
        if (type.GetDeclaringType() is { IsNil: false } declaring)
        {
            return Qualified(_md.GetTypeDefinition(declaring)) + "+" + name;
        }

        var space = _md.GetString(type.Namespace);
        return space.Length == 0 ? name : space + "." + name;
    }

    private string NameOf(EntityHandle handle) => handle.Kind switch
    {
        HandleKind.TypeDefinition => Qualified(_md.GetTypeDefinition((TypeDefinitionHandle)handle)),
        HandleKind.TypeReference => ReferenceName((TypeReferenceHandle)handle),
        HandleKind.TypeSpecification => _md.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(new TypeNames(this), genericContext: null),
        _ => "?",
    };

    private string ReferenceName(TypeReferenceHandle handle)
    {
        var reference = _md.GetTypeReference(handle);
        var name = _md.GetString(reference.Name);
        if (reference.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            return ReferenceName((TypeReferenceHandle)reference.ResolutionScope) + "+" + name;
        }

        var space = _md.GetString(reference.Namespace);
        return space.Length == 0 ? name : space + "." + name;
    }

    /// <summary>A name without its namespace, and without a generic's arity.</summary>
    internal static string Short(string fullName)
    {
        var name = fullName[(fullName.LastIndexOfAny(['.', '+']) + 1)..];
        var tick = name.IndexOf('`');
        return tick < 0 ? name : name[..tick];
    }

    /// <summary>Type names as WinRT spells them, without namespaces: <c>IReference&lt;Boolean&gt;</c> is <c>Boolean?</c>.</summary>
    private sealed class TypeNames(ApiReader reader) : ISignatureTypeProvider<string, object?>
    {
        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
        {
            PrimitiveTypeCode.Void => "void",
            PrimitiveTypeCode.Boolean => "Boolean",
            PrimitiveTypeCode.Char => "Char",
            PrimitiveTypeCode.Byte => "UInt8",
            PrimitiveTypeCode.Int16 => "Int16",
            PrimitiveTypeCode.UInt16 => "UInt16",
            PrimitiveTypeCode.Int32 => "Int32",
            PrimitiveTypeCode.UInt32 => "UInt32",
            PrimitiveTypeCode.Int64 => "Int64",
            PrimitiveTypeCode.UInt64 => "UInt64",
            PrimitiveTypeCode.Single => "Single",
            PrimitiveTypeCode.Double => "Double",
            PrimitiveTypeCode.String => "String",
            PrimitiveTypeCode.Object => "Object",
            _ => typeCode.ToString(),
        };

        public string GetTypeFromDefinition(MetadataReader md, TypeDefinitionHandle handle, byte rawTypeKind) => Short(reader.NameOf(handle));

        public string GetTypeFromReference(MetadataReader md, TypeReferenceHandle handle, byte rawTypeKind) => Short(reader.NameOf(handle));

        public string GetTypeFromSpecification(MetadataReader md, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
            md.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) =>
            genericType is "IReference" or "Nullable" && typeArguments.Length == 1
                ? typeArguments[0] + "?"
                : $"{genericType}<{string.Join(", ", typeArguments)}>";

        public string GetSZArrayType(string elementType) => elementType + "[]";

        public string GetArrayType(string elementType, ArrayShape shape) => elementType + "[" + new string(',', shape.Rank - 1) + "]";

        public string GetByReferenceType(string elementType) => "ref " + elementType;

        public string GetPointerType(string elementType) => elementType + "*";

        public string GetPinnedType(string elementType) => elementType;

        public string GetGenericMethodParameter(object? genericContext, int index) => "!!" + index;

        public string GetGenericTypeParameter(object? genericContext, int index) => "!" + index;

        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;

        public string GetFunctionPointerType(MethodSignature<string> signature) => "fnptr";
    }
}

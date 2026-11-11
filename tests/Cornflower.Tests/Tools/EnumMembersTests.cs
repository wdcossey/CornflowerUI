using WinRtApi;

// Enums shaped as Windows.winmd declares Symbol and a flags enum, for `winrt-api enum`: the reader
// reads this assembly's metadata in place of the SDK's. The values are made up.
namespace Windows.UI.Xaml.Controls
{
    public enum FixtureSymbol
    {
        Previous = 100,
        Next = 101,
        Pin = 105,
        Back = 99,
    }

    namespace Primitives
    {
        /// <summary>WinRT flags enums are UInt32, and can use the top bit.</summary>
        [Flags]
        public enum FixtureFlags : uint
        {
            None = 0,
            Top = 0x80000000,
        }
    }
}

namespace Cornflower.Tests.Tools
{
    public class EnumMembersTests
    {
        private static string Metadata => typeof(EnumMembersTests).Assembly.Location;

        [Fact]
        public void An_Enums_Members_Come_With_Their_Values_In_The_Metadatas_Order()
        {
            using var api = new ApiReader(Metadata);

            Assert.Equal([("Previous", 100m), ("Next", 101m), ("Pin", 105m), ("Back", 99m)], api.EnumMembers("Windows.UI.Xaml.Controls.FixtureSymbol"));
        }

        [Fact]
        public void An_Unsigned_Value_Is_Read_As_Unsigned()
        {
            using var api = new ApiReader(Metadata);

            Assert.Equal([("None", 0m), ("Top", 2147483648m)], api.EnumMembers("Windows.UI.Xaml.Controls.Primitives.FixtureFlags"));
        }

        // As for checklist: a full name, or a name under Controls, then Primitives.
        [Theory]
        [InlineData("FixtureSymbol", 4)]
        [InlineData("FixtureFlags", 2)]
        public void A_Short_Name_Resolves_As_For_Checklist(string type, int count)
        {
            using var api = new ApiReader(Metadata);

            Assert.Equal(count, api.EnumMembers(ChecklistCommand.Resolve(api, type)).Count);
        }

        [Theory]
        [InlineData("Windows.UI.Xaml.Controls.Button")] // a class: ChecklistFixture's
        [InlineData("NoSuchEnum")]
        public void Anything_But_An_Enum_Is_Refused(string type)
        {
            using var api = new ApiReader(Metadata);

            Assert.Throws<ArgumentException>(() => api.EnumMembers(type));
        }
    }
}

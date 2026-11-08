using System;
using System.Globalization;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace RefApp
{
    /// <summary>
    /// Writes an element and its whole visual tree as JSON: type, name, bounds relative to the specimen, and
    /// every resolved value a theme decides (brushes, thicknesses, fonts, transforms). This is what the
    /// Avalonia theme is measured against, alongside the PNG.
    /// </summary>
    internal static class TreeDump
    {
        private const int MaxDepth = 48;

        public static void Write(Json j, DependencyObject node, UIElement origin, int depth = 0)
        {
            j.Obj();
            j.Prop("type", node.GetType().FullName);

            var fe = node as FrameworkElement;
            if (fe != null)
            {
                if (!string.IsNullOrEmpty(fe.Name))
                {
                    j.Prop("name", fe.Name);
                }

                Bounds(j, fe, origin);
                j.Prop("visibility", fe.Visibility.ToString());
                if (fe.Opacity != 1)
                {
                    j.Prop("opacity", fe.Opacity);
                }

                Thick(j, "margin", fe.Margin);
                Size(j, "width", fe.Width);
                Size(j, "height", fe.Height);
                if (fe.MinWidth > 0) j.Prop("minWidth", fe.MinWidth);
                if (fe.MinHeight > 0) j.Prop("minHeight", fe.MinHeight);
                Size(j, "maxWidth", fe.MaxWidth);
                Size(j, "maxHeight", fe.MaxHeight);
                j.Prop("hAlign", fe.HorizontalAlignment.ToString());
                j.Prop("vAlign", fe.VerticalAlignment.ToString());
            }

            var control = node as Control;
            if (control != null)
            {
                Brush(j, "background", control.Background);
                Brush(j, "foreground", control.Foreground);
                Brush(j, "borderBrush", control.BorderBrush);
                Thick(j, "borderThickness", control.BorderThickness);
                Thick(j, "padding", control.Padding);
                Font(j, control.FontSize, control.FontFamily, control.FontWeight, control.FontStyle, control.CharacterSpacing);
                j.Prop("hContentAlign", control.HorizontalContentAlignment.ToString());
                j.Prop("vContentAlign", control.VerticalContentAlignment.ToString());
                j.Prop("isEnabled", control.IsEnabled);
            }

            var border = node as Border;
            if (border != null)
            {
                Brush(j, "background", border.Background);
                Brush(j, "borderBrush", border.BorderBrush);
                Thick(j, "borderThickness", border.BorderThickness);
                Thick(j, "padding", border.Padding);
                var r = border.CornerRadius;
                if (r.TopLeft != 0 || r.TopRight != 0 || r.BottomRight != 0 || r.BottomLeft != 0)
                {
                    j.Key("cornerRadius").Arr().Val(r.TopLeft).Val(r.TopRight).Val(r.BottomRight).Val(r.BottomLeft).EndArr();
                }
            }

            var panel = node as Panel;
            if (panel != null)
            {
                Brush(j, "background", panel.Background);
            }

            var text = node as TextBlock;
            if (text != null)
            {
                j.Prop("text", text.Text);
                Brush(j, "foreground", text.Foreground);
                Font(j, text.FontSize, text.FontFamily, text.FontWeight, text.FontStyle, text.CharacterSpacing);
                if (text.LineHeight > 0) j.Prop("lineHeight", text.LineHeight);
                j.Prop("lineStackingStrategy", text.LineStackingStrategy.ToString());
                j.Prop("textLineBounds", text.TextLineBounds.ToString());
                j.Prop("opticalMarginAlignment", text.OpticalMarginAlignment.ToString());
                j.Prop("textWrapping", text.TextWrapping.ToString());
                j.Prop("textTrimming", text.TextTrimming.ToString());
                Thick(j, "padding", text.Padding);
            }

            var presenter = node as ContentPresenter;
            if (presenter != null)
            {
                Brush(j, "foreground", presenter.Foreground);
                Font(j, presenter.FontSize, presenter.FontFamily, presenter.FontWeight, presenter.FontStyle, presenter.CharacterSpacing);
                if (presenter.Content != null) j.Prop("contentType", presenter.Content.GetType().FullName);
            }

            var shape = node as Shape;
            if (shape != null)
            {
                Brush(j, "fill", shape.Fill);
                Brush(j, "stroke", shape.Stroke);
                j.Prop("strokeThickness", shape.StrokeThickness);
                j.Prop("stretch", shape.Stretch.ToString());
                var rect = node as Rectangle;
                if (rect != null && (rect.RadiusX != 0 || rect.RadiusY != 0))
                {
                    j.Key("radius").Arr().Val(rect.RadiusX).Val(rect.RadiusY).EndArr();
                }
            }

            var ui = node as UIElement;
            if (ui != null)
            {
                Transform(j, ui.RenderTransform);
                if (ui.RenderTransformOrigin.X != 0 || ui.RenderTransformOrigin.Y != 0)
                {
                    j.Key("renderTransformOrigin").Arr().Val(ui.RenderTransformOrigin.X).Val(ui.RenderTransformOrigin.Y).EndArr();
                }

                Projection(j, ui.Projection);
                var clip = ui.Clip;
                if (clip != null)
                {
                    j.Key("clip").Arr().Val(clip.Rect.X).Val(clip.Rect.Y).Val(clip.Rect.Width).Val(clip.Rect.Height).EndArr();
                }
            }

            var count = depth < MaxDepth ? VisualTreeHelper.GetChildrenCount(node) : 0;
            if (count > 0)
            {
                j.Key("children").Arr();
                for (var i = 0; i < count; i++)
                {
                    Write(j, VisualTreeHelper.GetChild(node, i), origin, depth + 1);
                }

                j.EndArr();
            }

            j.EndObj();
        }

        public static void Bounds(Json j, FrameworkElement fe, UIElement origin, string key = "bounds")
        {
            try
            {
                var r = fe.TransformToVisual(origin).TransformBounds(new Rect(0, 0, fe.ActualWidth, fe.ActualHeight));
                j.Key(key).Arr().Val(r.X).Val(r.Y).Val(r.Width).Val(r.Height).EndArr();
            }
            catch (Exception)
            {
                // Not in the same tree, or not laid out: leave the bounds out rather than guess.
            }
        }

        public static string Colour(Color c) =>
            string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", c.A, c.R, c.G, c.B);

        public static void Brush(Json j, string key, Brush brush)
        {
            if (brush == null)
            {
                j.Key(key).Val((string)null);
                return;
            }

            var solid = brush as SolidColorBrush;
            var value = solid != null ? Colour(solid.Color) : brush.GetType().FullName;
            if (brush.Opacity != 1)
            {
                value += "@" + Math.Round(brush.Opacity, 4).ToString(CultureInfo.InvariantCulture);
            }

            j.Prop(key, value);
        }

        private static void Thick(Json j, string key, Thickness t)
        {
            if (t.Left == 0 && t.Top == 0 && t.Right == 0 && t.Bottom == 0)
            {
                return;
            }

            j.Key(key).Arr().Val(t.Left).Val(t.Top).Val(t.Right).Val(t.Bottom).EndArr();
        }

        private static void Size(Json j, string key, double value)
        {
            if (!double.IsNaN(value) && !double.IsInfinity(value))
            {
                j.Prop(key, value);
            }
        }

        private static void Font(Json j, double size, FontFamily family, Windows.UI.Text.FontWeight weight, Windows.UI.Text.FontStyle style, int spacing)
        {
            j.Prop("fontSize", size);
            j.Prop("fontFamily", family != null ? family.Source : null);
            j.Prop("fontWeight", weight.Weight);
            if (style != Windows.UI.Text.FontStyle.Normal) j.Prop("fontStyle", style.ToString());
            if (spacing != 0) j.Prop("characterSpacing", spacing);
        }

        private static void Transform(Json j, Transform t)
        {
            if (t == null)
            {
                return;
            }

            var composite = t as CompositeTransform;
            if (composite != null)
            {
                if (composite.ScaleX == 1 && composite.ScaleY == 1 && composite.TranslateX == 0 && composite.TranslateY == 0 && composite.Rotation == 0 && composite.SkewX == 0 && composite.SkewY == 0)
                {
                    return;
                }

                j.Key("renderTransform").Obj()
                    .Prop("kind", "Composite")
                    .Prop("scaleX", composite.ScaleX).Prop("scaleY", composite.ScaleY)
                    .Prop("translateX", composite.TranslateX).Prop("translateY", composite.TranslateY)
                    .Prop("rotation", composite.Rotation)
                    .Prop("centerX", composite.CenterX).Prop("centerY", composite.CenterY)
                    .EndObj();
                return;
            }

            var translate = t as TranslateTransform;
            if (translate != null)
            {
                if (translate.X != 0 || translate.Y != 0)
                {
                    j.Key("renderTransform").Obj().Prop("kind", "Translate").Prop("x", translate.X).Prop("y", translate.Y).EndObj();
                }

                return;
            }

            var scale = t as ScaleTransform;
            if (scale != null)
            {
                if (scale.ScaleX != 1 || scale.ScaleY != 1)
                {
                    j.Key("renderTransform").Obj().Prop("kind", "Scale").Prop("scaleX", scale.ScaleX).Prop("scaleY", scale.ScaleY)
                        .Prop("centerX", scale.CenterX).Prop("centerY", scale.CenterY).EndObj();
                }

                return;
            }

            var rotate = t as RotateTransform;
            if (rotate != null)
            {
                if (rotate.Angle != 0)
                {
                    j.Key("renderTransform").Obj().Prop("kind", "Rotate").Prop("angle", rotate.Angle)
                        .Prop("centerX", rotate.CenterX).Prop("centerY", rotate.CenterY).EndObj();
                }

                return;
            }

            var matrix = t as MatrixTransform;
            if (matrix != null)
            {
                var m = matrix.Matrix;
                if (!m.IsIdentity)
                {
                    j.Key("renderTransform").Obj().Prop("kind", "Matrix")
                        .Key("matrix").Arr().Val(m.M11).Val(m.M12).Val(m.M21).Val(m.M22).Val(m.OffsetX).Val(m.OffsetY).EndArr()
                        .EndObj();
                }

                return;
            }

            j.Key("renderTransform").Obj().Prop("kind", t.GetType().FullName).EndObj();
        }

        private static void Projection(Json j, Projection p)
        {
            var plane = p as PlaneProjection;
            if (plane == null)
            {
                if (p != null)
                {
                    j.Key("projection").Obj().Prop("kind", p.GetType().FullName).EndObj();
                }

                return;
            }

            if (plane.RotationX == 0 && plane.RotationY == 0 && plane.RotationZ == 0 && plane.GlobalOffsetZ == 0 && plane.LocalOffsetZ == 0 && plane.GlobalOffsetX == 0 && plane.GlobalOffsetY == 0)
            {
                return;
            }

            j.Key("projection").Obj()
                .Prop("kind", "Plane")
                .Prop("rotationX", plane.RotationX).Prop("rotationY", plane.RotationY).Prop("rotationZ", plane.RotationZ)
                .Prop("centerX", plane.CenterOfRotationX).Prop("centerY", plane.CenterOfRotationY).Prop("centerZ", plane.CenterOfRotationZ)
                .Prop("globalOffsetX", plane.GlobalOffsetX).Prop("globalOffsetY", plane.GlobalOffsetY).Prop("globalOffsetZ", plane.GlobalOffsetZ)
                .Prop("localOffsetZ", plane.LocalOffsetZ)
                .EndObj();
        }
    }
}

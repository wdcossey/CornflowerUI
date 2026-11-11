using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Cornflower.Controls;

namespace Cornflower.Tests.Behaviours;

/// <summary>
/// <see cref="TiltModel"/>'s drawing against Avalonia's own 3D transform, and its step against its
/// drawing. The readings themselves are in <see cref="TiltTests"/>.
/// </summary>
public class TiltModelTests
{
    private static readonly Size Size = new(300, 120);

    private static readonly Point[] Points = [new(0, 0), new(300, 0), new(0, 120), new(300, 120), new(150, 60), new(37, 101)];

    // Avalonia transforms points in single precision.
    private const double Precision = 1e-4;

    private static Point Draw(Matrix matrix, Point p) => matrix.Transform(p);

    // With no depth and the eye over the centre, the tilt is Avalonia's Rotate3DTransform about the
    // centre, with the angles' signs turned (PlaneProjection's and Avalonia's axes turn opposite
    // ways) and Depth the eye's distance. The model adds what Rotate3DTransform can't: a depth
    // offset between the rotation and the perspective, and an eye away from the centre.
    [AvaloniaTheory]                                                // Rotate3DTransform is an AvaloniaObject: on the UI thread
    [InlineData(-8.527, 8.233)]
    [InlineData(7.6, 0)]
    [InlineData(0, -8.1)]
    public void With_The_Eye_Centred_And_No_Depth_It_Is_Avalonias_Rotate3DTransform(double rotationX, double rotationY)
    {
        var centre = new Point(Size.Width / 2, Size.Height / 2);
        var model = TiltModel.ToMatrix(new TiltState(rotationX, rotationY, 0), Size, centre);
        // Rotate3DTransform is drawn about RenderTransformOrigin, the centre by default, with its eye there.
        var avalonia = new Rotate3DTransform(-rotationX, -rotationY, 0, 0, 0, 0, TiltModel.EyeDistance).Value;

        foreach (var p in Points)
        {
            var ours = Draw(model, p);
            var theirs = Draw(avalonia, p - centre) + centre;
            Assert.Equal(theirs.X, ours.X, 1e-3);
            Assert.Equal(theirs.Y, ours.Y, 1e-3);
        }
    }

    // A depth of z with the eye at distance d draws the element at d / (d - z) of its size, about the point under the eye.
    [Fact]
    public void Depth_Shrinks_The_Element_Towards_The_Point_Under_The_Eye()
    {
        var eye = new Point(400, 900);
        var model = TiltModel.ToMatrix(new TiltState(0, 0, -25), Size, eye);
        var scale = TiltModel.EyeDistance / (TiltModel.EyeDistance + 25);

        foreach (var p in Points)
        {
            var drawn = Draw(model, p);
            var expected = eye + (p - eye) * scale;
            Assert.Equal(expected.X, drawn.X, Precision);
            Assert.Equal(expected.Y, drawn.Y, Precision);
        }
    }

    // The step is the inverse of the drawing: the point the model unprojects from where a point is drawn is that point.
    [Theory]
    [InlineData(-8.527, 8.233, -0.623, 117, 200)]
    [InlineData(4.1, -4.35, -12.7, -45, -250)]
    [InlineData(-0.35, 0.06, -24.4, 0, 0)]
    public void Unproject_Undoes_The_Drawing(double rotationX, double rotationY, double offsetZ, double eyeX, double eyeY)
    {
        var tilt = new TiltState(rotationX, rotationY, offsetZ);
        var eye = new Point(Size.Width / 2 + eyeX, Size.Height / 2 + eyeY);
        var model = TiltModel.ToMatrix(tilt, Size, eye);

        foreach (var p in Points)
        {
            var back = TiltModel.Unproject(tilt, Size, Draw(model, p), eye);
            Assert.Equal(p.X, back.X, Precision);
            Assert.Equal(p.Y, back.Y, Precision);
        }
    }

    // The toolkit's rule, before any step: 0.3 rad times the offset, so 8.59° at an edge, the full 25
    // deep at the centre and none at a corner. Past the edge, the offset is the edge's.
    [Theory]
    [InlineData(0, 0, 0, 0, -25)]
    [InlineData(-0.5, 0, 0, 8.594366926962348, -12.5)]
    [InlineData(0.25, -0.25, -4.297183463481174, -4.297183463481174, -12.5)]
    [InlineData(0.5, 0.5, 8.594366926962348, -8.594366926962348, 0)]
    [InlineData(0.9, 0, 0, -8.594366926962348, -12.5)]
    public void The_Rule_Is_The_Toolkits(double dx, double dy, double rotationX, double rotationY, double offsetZ)
    {
        var tilt = TiltModel.ForOffset(dx, dy);
        Assert.Equal(rotationX, tilt.RotationX, 1e-9);
        Assert.Equal(rotationY, tilt.RotationY, 1e-9);
        Assert.Equal(offsetZ, tilt.OffsetZ, 1e-9);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(180, 1)]
    [InlineData(277.5, 0.29289321881345254)]                       // halfway: 1 - sin(π / 4)
    [InlineData(375, 0)]
    public void The_Return_Holds_Then_Eases_Out_On_A_Sine(double sinceReleaseMs, double left)
    {
        Assert.Equal(left, TiltModel.ReturnFactor(TimeSpan.FromMilliseconds(sinceReleaseMs)), 1e-9);
    }
}

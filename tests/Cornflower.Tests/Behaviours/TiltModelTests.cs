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

    // An element 60,000 px across, pressed 10 px from its left end, 7,000 px from the eye: once
    // tilted, the line from the eye through the press meets the plane behind the eye, and the
    // steps swung between corners every frame. The press is taken as it is, and the step settles.
    [Fact]
    public void A_Press_No_Point_Of_The_Tilted_Element_Is_Drawn_Under_Still_Settles()
    {
        var size = new Size(60000, 100);
        var eye = TiltModel.Eye(size, new Vector(225 - 30000, 350));
        var press = new Point(10, 50);
        var tilt = TiltState.Rest;

        for (var frame = 0; frame < 10; frame++)
        {
            var next = TiltModel.Step(tilt, size, press, eye);
            Assert.True(double.IsFinite(next.RotationX) && double.IsFinite(next.RotationY) && double.IsFinite(next.OffsetZ), $"{next}");
            var moved = Math.Abs(next.RotationX - tilt.RotationX) + Math.Abs(next.RotationY - tilt.RotationY) +
                        Math.Abs(next.OffsetZ - tilt.OffsetZ);
            tilt = next;
            if (moved <= TiltModel.Settled)
            {
                Assert.Equal(TiltModel.ForOffset(press.X / size.Width - 0.5, 0), tilt);
                return;
            }
        }

        Assert.Fail($"not settled after 10 frames: {tilt}");
    }

    // Stepping isn't sure to settle: for TiltTests' huge Button, pressed where it is, the steps go
    // on swinging well past TiltModel.MaxSteps (with and without the fallback above). So the tilt
    // stops itself after that many frames.
    [Fact]
    public void Some_Presses_Never_Settle_By_Stepping()
    {
        var size = TiltTests.HugeSize;
        var at = TiltTests.HugeAt;
        var eye = TiltModel.Eye(size, new Point(225, 400) - (at + new Point(size.Width / 2, size.Height / 2)));
        var press = TiltTests.HugePress - at;
        var tilt = TiltState.Rest;

        for (var frame = 0; frame < 10 * TiltModel.MaxSteps; frame++)
        {
            var next = TiltModel.Step(tilt, size, (Point)press, eye);
            var moved = Math.Abs(next.RotationX - tilt.RotationX) + Math.Abs(next.RotationY - tilt.RotationY) +
                        Math.Abs(next.OffsetZ - tilt.OffsetZ);
            Assert.True(moved > TiltModel.Settled, $"settled at frame {frame}");
            tilt = next;
        }
    }

    // The emulator's release in animation time: frame k after the release at (k − 1) × 15.625 ms.
    // Held until the first change, then each frame's fraction as every release read it. With #60's
    // sine (a 180 ms hold, 195 ms back) frame 19 would read 0.272, not 0.303, and frame 24 not be at rest.
    [Fact]
    public void The_Return_Matches_The_Emulators_Frames()
    {
        for (var frame = 1; frame < TiltReadings.ReleaseFirstFrame; frame++)
            Assert.Equal(1, TiltModel.ReturnFactor(TimeSpan.FromMilliseconds((frame - 1) * 15.625)));

        for (var i = 0; i < TiltReadings.ReleaseFractions.Length; i++)
        {
            var at = TimeSpan.FromMilliseconds((TiltReadings.ReleaseFirstFrame - 1 + i) * 15.625);
            var left = TiltModel.ReturnFactor(at);
            Assert.True(Math.Abs(left - TiltReadings.ReleaseFractions[i]) <= 0.0011,           // the worst, 0.0010
                $"frame {TiltReadings.ReleaseFirstFrame + i}: {left:F5} left, read {TiltReadings.ReleaseFractions[i]}");
        }
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(182.6, 1)]
    [InlineData(349.6, 0)]
    [InlineData(1000, 0)]
    public void The_Return_Holds_Then_Ends(double sinceReleaseMs, double left)
    {
        Assert.Equal(left, TiltModel.ReturnFactor(TimeSpan.FromMilliseconds(sinceReleaseMs)), 1e-9);
    }
}

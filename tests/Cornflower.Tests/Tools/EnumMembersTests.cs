using WinRtApi;

// An enum shaped as Windows.winmd declares Symbol, for `winrt-api enum`: the reader reads this
// assembly's metadata in place of the SDK's. The values are made up.
namespace Windows.UI.Xaml.Controls
{
    public enum FixtureSymbol
    {
        Previous = 100,
        Next = 101,
        Pin = 105,
        Back = 99,
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

            Assert.Equal([("Previous", 100L), ("Next", 101L), ("Pin", 105L), ("Back", 99L)], api.EnumMembers("Windows.UI.Xaml.Controls.FixtureSymbol"));
        }

        [Fact]
        public void A_Short_Name_Is_Looked_Up_Under_Controls()
        {
            using var api = new ApiReader(Metadata);

            Assert.Equal(4, api.EnumMembers("FixtureSymbol").Count);
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

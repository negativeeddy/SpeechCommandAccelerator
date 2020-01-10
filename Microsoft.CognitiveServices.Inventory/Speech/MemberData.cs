using System.Xml.Linq;
using Xamarin.Forms;

namespace Microsoft.CognitiveServices.Inventory
{
    public class MemberData
    {
        public string Name { get; set; }
        public Color Color { get; set; }

        public override string ToString()
        {
            return $"MemberData{{name={Name}, color={Color}}}";
        }
    }
}
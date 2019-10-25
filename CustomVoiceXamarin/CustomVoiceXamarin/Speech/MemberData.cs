using System.Xml.Linq;

namespace CustomVoiceXamarin
{
    public class MemberData
    {
        public string Name { get; set; }
        public string Color { get; set; }

        public override string ToString()
        {
            return $"MemberData{{name={Name}, color={Color}}}";
        }
    }
}
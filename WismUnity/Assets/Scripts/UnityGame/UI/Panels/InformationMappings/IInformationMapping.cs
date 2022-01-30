using Wism.Client.Core;

namespace Assets.Scripts.UI
{
    public interface IInformationMapping
    {
        public void GetLabelValuePair(int index, Tile subject, out string label, out string value);

        public bool CanMapSubject(Tile subject);
    }
}

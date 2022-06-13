namespace Furball.Vixie.Backends.Shared.Backends {
    public class FeatureLevel {
        public string Name;
        public string Description;
        public object Value;

        public bool Boolean => (bool) this.Value;
        public string String => (string) this.Value;
        public int Integer => (int) this.Value;
    }
}

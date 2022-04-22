using System.Collections.Generic;
using System.Text;
using Kettu;

namespace Furball.Vixie.Backends.Shared.Backends {
    public class BackendInfoSection {
        public BackendInfoSection(string name) {
            this.Name = name;
        }
        
        public string Name;
        
        public List<(string name, string info)> Contents = new();

        public void Log(LoggerLevel level) {
            string begin = $"///// {Name} /////";
            Logger.Log(begin, level);

            foreach ((string? name, string? info) in this.Contents) {
                Logger.Log($"{name}: {info}", level);
            }
            
            StringBuilder endcap = new();
            for (int i = 0; i < begin.Length; i++) {
                endcap.Append("/");
            }
            Logger.Log(endcap.ToString(), level);
        }
    }
}

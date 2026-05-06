using System.Collections.Generic;

namespace Desktop_Container
{
    public class SaveDatas
    {
        public string Title { get; set; } = "Container";
        public bool Reduced { get; set; } = false;
        public bool BottomTitleBar { get; set; } = false;
        public string Color { get; set; } = "#4C202020";
        public List<int> Size { get; set; } = [400, 400];
        public List<int> Position { get; set; } = [0, 0];
        public List<string> Files { get; set; } = [];
        public string ?LinkedDirectory { get; set; }
    }
}

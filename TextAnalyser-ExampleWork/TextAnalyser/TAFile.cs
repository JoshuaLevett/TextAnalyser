using System.Collections.Generic;

namespace TextAnalyser
{
    public class TAFile
    {
        public string FileName;
        public List<int> FileContent;
        public int[] WordCounts;

        public TAFile()
        {
            FileContent = new List<int>();
        }

        public TAFile(List<int> content)
        {
            FileContent = content;
        }
    }
}
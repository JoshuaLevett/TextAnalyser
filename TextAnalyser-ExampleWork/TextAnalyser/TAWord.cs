namespace TextAnalyser
{
    public class TAWord
    {
        public int Id;
        public int Count;
        public string Text;
        public float Value;

        public TAWord(int id, string text)
        {
            Id = id;
            Text = text;
            Count = 1;
            Value = 0f;
        }
    }
}
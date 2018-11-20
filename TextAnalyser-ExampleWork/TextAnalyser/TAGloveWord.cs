using System;
using System.Data.SqlTypes;
using System.Linq;

namespace TextAnalyser
{
    public class TAGloveWord
    {
        public string Text;
        public float[] Vecs;
        public float[] NormVecs;

        public TAGloveWord(string word, float[] vectors)
        {
            Text = word;
            Vecs = vectors;
            NormVecs = new float[vectors.Length];
            //magnitude = all vectors squared
            double magnitude = Vecs.Aggregate(0f, (current, vec) => current + (vec*vec));
            magnitude = Math.Sqrt(magnitude);
            //magnitude now = the length of the vectors combined
            //magnitude is now what every vector will be divided by to become normalised
            for (var x = 0; x < Vecs.Length; x++)
            {
                NormVecs[x] = (float) (Vecs[x]/magnitude);
            }
            //NormVecs is now created
            //NormVecs ensures that no vector has a magnitude higher than 1
        }
    }
}
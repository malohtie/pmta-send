using System.Collections.Generic;

namespace Send.helpers
{
    class Rotation
    {
        public int Index { get; set; }
        public int RotateEvery { get; set; }
        public List<dynamic> Data { get; set; }
        public int Conter { get; set; }

        public Rotation(dynamic Data, int RotateEvery = 100)
        {
            Index = 0;
            Conter = 1;
            this.Data = new List<dynamic>(Data);
            this.RotateEvery = RotateEvery;
        }

        public string GetCurrent()
        {
            return Data[Index];
        }

        public string GetAndRotate()
        {
            string ReplyMail = Data[Index];
            Conter++;

            if (Conter % RotateEvery == 0)
            {
                if (Index >= (Data.Count - 1))
                {
                    Index = 0;
                }
                else
                {
                    Index++;
                }
            }
            return ReplyMail;
        }
    }
}

using System;
using System.Collections.Generic;

namespace Send.helpers
{
    class Rotation
    {
        public int Index { get; set; }
        public int RotateEvery { get; set; }
        public List<dynamic> Data { get; set; }
        public int Counter { get; set; }

        public Rotation(dynamic Data, int RotateEvery = 100)
        {
            Index = 0;
            Counter = 1;
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
            Counter++;

            if (Counter % RotateEvery == 0)
            {
                Index = Index >= (Data.Count - 1) ? 0 : Index++;
            }
            return ReplyMail;
        }
        public string ThreadGetAndRotate()
        {
            lock (this)
            {
                string email = Data[Index];
                Counter++;
                if (Counter % RotateEvery == 0)
                {
                    Index = Index >= (Data.Count - 1) ? 0 : Index++;
                }
                return email;
            }
        }
    }
}

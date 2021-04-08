using Newtonsoft.Json;

namespace Send.helpers
{
    class Placeholder
    {
        public int[] Index { get; set; }
        public int RotateEvery { get; set; }
        public dynamic Data { get; set; }
        public int Conter { get; set; }
        public Placeholder(dynamic Data, int RotateEvery = 100)
        {
            Conter = 1;
            this.Data = JsonConvert.DeserializeObject(Data);
            this.RotateEvery = RotateEvery;
            Index = new int[this.Data.Count];
        }

        public string GetCurrent(int key)
        {
            return Data[key][Index[key]];
        }

        public string GetAndRotate(int key)
        {
            string ReplyMail = Data[key][Index[key]];
            Conter++;

            if (Conter % RotateEvery == 0)
            {
                if (Index[key] >= (Data[key].Count - 1))
                {
                    Index[key] = 0;
                }
                else
                {
                    Index[key]++;
                }
            }
            return ReplyMail;
        }

        public string ThreadGetAndRotate(int key)
        {
            lock (this)
            {
                string ReplyMail = Data[key][Index[key]];
                Conter++;

                if (Conter % RotateEvery == 0)
                {
                    if (Index[key] >= (Data[key].Count - 1))
                    {
                        Index[key] = 0;
                    }
                    else
                    {
                        Index[key]++;
                    }
                }
                return ReplyMail;
            }
        }
    }
}

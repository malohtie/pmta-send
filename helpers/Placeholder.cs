using Newtonsoft.Json;
using port25.pmta.api.submitter;
using System.Text.RegularExpressions;

namespace Send.helpers
{
    class Placeholder
    {
        public int[] Index { get; set; }
        public int RotateEvery { get; set; }
        public dynamic Data { get; set; }
        public Placeholder(dynamic Data, int RotateEvery = 100)
        {
            this.Data = JsonConvert.DeserializeObject(Data);
            this.RotateEvery = RotateEvery;
            Index = new int[this.Data.Count];
        }

        private int Size()
        {
            return (int)Data.Count;
        }

        private string GetCurrent(int key)
        {
            return Data[key][Index[key]];
        }

        private string GetAndRotate(int key, int counter)
        {
            string ReplyMail = Data[key][Index[key]];
            if (counter % RotateEvery == 0)
            {
                Index[key] = Index[key] >= (Data[key].Count - 1) ? 0 : Index[key]++;
            }
            return ReplyMail;
        }

        private string TheadGetAndRotate(int key, int counter)
        {
            lock (this)
            {
                string ReplyMail = Data[key][Index[key]];
                if (counter % RotateEvery == 0)
                {
                    Index[key] = Index[key] >= (Data[key].Count - 1) ? 0 : Index[key]++;                    
                }
                return ReplyMail;
            }
        }

        public string ReplaceRotate(string data, int counter, bool thread = false)
        {
            for (int i = 0; i < Size(); i++)
            {
                int index = i + 1;
                if(thread)
                {
                    data = Regex.Replace(data, $"[placeholder_{index}]", TheadGetAndRotate(i, counter), RegexOptions.IgnoreCase);
                }
                else
                {
                    data = Regex.Replace(data, $"[placeholder_{index}]", GetAndRotate(i, counter), RegexOptions.IgnoreCase);
                }
               
            }
            return data;

        }

        public string ReplaceCurrent(string data)
        {
            for (int i = 0; i < Size(); i++)
            {
                int index = i + 1;
                data = Regex.Replace(data, $"[placeholder_{index}]", GetCurrent(i), RegexOptions.IgnoreCase);

            }
            return data;

        }

        public Recipient ReplaceRotateReciption(Recipient data, int counter, bool thread = false)
        {
            for (int i = 0; i < Size(); i++)
            {
                int index = i + 1;
                if (thread)
                {
                    data[$"[placeholder_{index}]"] = TheadGetAndRotate(i, counter);
                   
                }
                else
                {
                    data[$"[placeholder_{index}]"] = GetAndRotate(i, counter);
                }

            }
            return data;
        }

        public Recipient ReplaceCurrentReciption(Recipient data)
        {
            for (int i = 0; i < Size(); i++)
            {
                int index = i + 1;
                data[$"[placeholder_{index}]"] = GetCurrent(i);

            }
            return data;
        }
    }
}

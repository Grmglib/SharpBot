namespace SharpBot
{
    public static class SavingPlugin
    {
        public static void SaveVariable(string savename, TypeCode tc, params object[] value)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + @"\" + savename + "." + tc.ToString();

            if (!File.Exists(path))
            {
                var myfile = File.Create(path);
                myfile.Close();
                string val =$"{value.ToString()}\n";
                File.WriteAllText(path, val);
                File.SetAttributes(path, FileAttributes.Hidden);
            }
            else
            {
                string txt = "";
                try
                {
                    txt = File.ReadAllText(path);
                    File.SetAttributes(path, FileAttributes.Normal);
                    string val = null;
                    foreach (var item in value) 
                    {
                        val += $"{item.ToString()}\n";
                    }
                    File.WriteAllText(path, val);
                    File.WriteAllText(path, val);
                    File.SetAttributes(path, FileAttributes.Hidden);
                }
                catch (Exception ex)
                {
                    Console.Write(ex);
                }
            }
        }

        public static object GetVariable(string savename, TypeCode tc)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + @"\" + savename + "." + tc.ToString();
            File.SetAttributes(path, FileAttributes.Normal);
            string txt = "";

            try
            {
                txt = File.ReadAllText(path);
                File.SetAttributes(path, FileAttributes.Hidden);
                var value = Convert.ChangeType(txt, tc);
                return value;
            }
            catch (Exception ex)
            {
                Console.Write(ex);
                return null;
            }
        }

        public static void DeleteVariable(string savename, TypeCode tc)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + @"\" + savename + "." + tc.ToString();
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
        }

        public static bool Exists(string savename, TypeCode tc)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + @"\" + savename + "." + tc.ToString();
            bool _true = true;

            try
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.SetAttributes(path, FileAttributes.Hidden);
            }
            catch
            {
                _true = false;
            }

            return _true;
        }
    }
}
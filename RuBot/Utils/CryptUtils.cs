using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RuBot.Utils
{
    public class CryptUtils
    {
        public static string DecryptFile(string fileName, byte[] password)
        {
            using (var fs = File.OpenRead(fileName))
            {
                var iv = new byte[16];
                fs.Read(iv, 0, 16);
                using (var aesAlg = new AesManaged())
                {
                    aesAlg.Key = password;
                    aesAlg.IV = iv;

                    var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                    using (var csDecrypt = new CryptoStream(fs, decryptor, CryptoStreamMode.Read))
                    {
                        using (var srDecrypt = new BinaryReader(csDecrypt))
                        {
                            const int bufferLength = 1024;
                            var sb = new StringBuilder();
                            var array = srDecrypt.ReadBytes(bufferLength);
                            sb.Append(Encoding.ASCII.GetChars(array));
                            while(array.Length == bufferLength)
                            {
                                array = srDecrypt.ReadBytes(bufferLength);
                                sb.Append(Encoding.ASCII.GetChars(array));    
                            }
                            return sb.ToString();
                        }
                    }
                }
            }
        }
        public static void EncryptFile(string text, string fileName, byte[] password)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);
            using (var aesAlg = new AesManaged())
            {
                aesAlg.Key = password;
                aesAlg.GenerateIV();

                // Create a decrytor to perform the stream transform.
                var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new BinaryWriter(csEncrypt))
                        {
                            swEncrypt.Write(aesAlg.IV,0,16);
                            swEncrypt.Write(Encoding.ASCII.GetBytes(text));
                        }
                        File.WriteAllBytes(fileName, msEncrypt.ToArray());
                    }
                }
            }
        }
    }
}

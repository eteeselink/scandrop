using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace ScanDrop
{
    public class LocalStorage
    {
        public static Task SaveObject<T>(string filename, T data)
        {
            return Task.Run(() =>
            {
                Save(filename, (stream) =>
                {
                    var serializer = new DataContractJsonSerializer(typeof(T));
                    serializer.WriteObject(stream, data);
                });
            });
        }

        public static Task<T> LoadObject<T>(string filename)
        {
            return Task.Run(() =>
            {
                return Load<T>(filename, (stream) =>
                {
                    var serializer = new DataContractJsonSerializer(typeof(T));
                    return (T)serializer.ReadObject(stream);
                });
            });
        }

        public static StorageFolder Folder
        {
            get
            {
                return Windows.Storage.ApplicationData.Current.LocalFolder;
            }
        }

        public static T Load<T>(string filename, Func<Stream, T> reader)
        {
            var folder = Folder;
            try
            {
                var file = folder.GetFileAsync(filename).AsTask().Result;
                using(var stream = file.OpenStreamForReadAsync().Result)
                { 
                    return reader(stream);
                }
            }
            catch (Exception)
            {
                // silently ignore failures: something is iffy.
            }

            return default(T);
        }

        public static void Save(string filename, Action<Stream> writer)
        {
            var folder = Folder;
            var file = folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting).AsTask().Result;
            using(var stream = file.OpenStreamForWriteAsync().Result)
            {
                writer(stream);
            }
        }
    }
}

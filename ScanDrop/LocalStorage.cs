using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

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

        public static T Load<T>(string filename, Func<Stream, T> reader)
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (store.FileExists(filename))
                {
                    using (var stream = new IsolatedStorageFileStream(filename, FileMode.OpenOrCreate, FileAccess.Read, store))
                    {
                        try
                        {
                            return reader(stream);
                        }
                        catch (Exception)
                        {
                            // silently ignore failures: something is iffy.
                        }
                    }
                }
            }
            return default(T);
        }

        public static void Save(string filename, Action<Stream> writer)
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            using (var stream = new IsolatedStorageFileStream(filename, FileMode.Create, FileAccess.Write, store))
            {
                writer(stream);
            }
        }

        public static void Delete(string filename)
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (store.FileExists(filename))
                {
                    store.DeleteFile(filename);
                }
            }
        }
    }
}

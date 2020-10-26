using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace P2P
{
    /// <summary>
    /// Helper class for managing objects that need to be synced
    /// Contains a reference to the local instance of the client.(if necessary)
    /// </summary>
    /// <typeparam name="T"> The type of the objects </typeparam>
    public class ObjSyncList<T> where T : ISyncObj {
        List<T> allObjs = new List<T>();
        public T localInstance;

        public ObjSyncList(T localInstance) {
            this.localInstance = localInstance;
            if(localInstance != null)
                allObjs.Add(this.localInstance);
        }

        // Convert the list to bytes
        public List<byte> GetBytesFromList() {
            List<byte> bytes = new List<byte>();
            foreach (T obj in allObjs) {
                List<byte> objData = obj.GetByteData();
                bytes.Add((byte)objData.Count); // Add the length of the object
                bytes.AddRange(objData); // Add the object
            }

            return bytes;
        }

        // Convert bytes to the list and callback what to do with the object info
        public void GetListFromBytes(RecievedPacket packet, Action<List<object>, float> callback) {
            int curI = 0;
            while (curI < packet.data.Count) {
                byte length = packet.data[curI];
                curI++;
                callback.Invoke(DataConverter.ConvertByteToObject<T>(packet.data.GetRange(curI, length)), packet.GetTimeDifference());
                curI += length;
            }
        }

        // Find object with the id
        public T GetObjWithId(byte id) {
            foreach(T obj in allObjs) {
                if(obj.id == id) {
                    return obj;
                }
            }
            return default;
        }

        // Add extra instance to the list
        public void Add(T obj) {
            if(!Contains(obj))
                allObjs.Add(obj);
        }

        // Remove instance from the list
        public void Remove(T obj) {
            allObjs.Remove(obj);
        }

        // Get the count of the objects
        public int Count() {
            return allObjs.Count;
        }

        // Check if object is in the list
        public bool Contains(T obj) {
            return allObjs.Contains(obj);
        }

        // Get an object with the given index
        public T Get(int i) {
            if (i < allObjs.Count && i >= 0)
                return allObjs[i];
            return default;
        } 

        // Get the complete list
        public List<T> GetList() {
            return allObjs;
        }

        // Execute a statement on all objects
        public void ExecStatement(Action<T> action) {
            foreach(T obj in allObjs) {
                action.Invoke(obj);
            }
        }

        // Execute a statement on all objects and returning an object
        public object ExecStatementReturn(Func<T, object> action) {
            foreach (T obj in allObjs) {
                return action.Invoke(obj);
            }
            return null;
        }

        // Reset the list and local instance
        public void Reset() {
            allObjs.Clear();
            localInstance = default;
        }

        // Debug
        public void DebugAllObj() {
            foreach(T obj in allObjs) {
                Debug.LogError(obj.id + " -- " + obj);
            }
            Debug.LogError(localInstance.id + " -- " + localInstance);
        }

        // Get the bytes of a object and sync it to a object
        public T UnpackAndSyncObj(RecievedPacket packet) {
            List<object> properties = DataConverter.ConvertByteToObject<T>(packet.data);
            T obj = GetObjWithId((byte)properties[0]);
            obj?.SyncDataToObj(properties, packet.GetTimeDifference());
            if (obj == null)
                Debug.LogError("Object was not found in the list and was not synchronized: " + obj);
            return obj;
        }

        // Get the object with the given bytes
        public T UnpackAndGetObj(List<byte> bytes) {
            List<object> properties = DataConverter.ConvertByteToObject<T>(bytes);
            return GetObjWithId((byte)properties[0]);
        }

        // Get the properties of the given object
        public List<object> UnpackObj(List<byte> bytes) {
            return DataConverter.ConvertByteToObject<T>(bytes);
        }
    }

    /// <summary>
    /// Helper class to convert several objects into bytes and back
    /// </summary>
    public class DataConverter
    {
        /// <summary>
        /// Convert object to byte data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"> Instance of the object which should be converted </param>
        /// <returns></returns>
        public static List<byte> ConvertObjectToByte<T>(T obj) {
            List<byte> bytes = new List<byte>();

            PropertyInfo[] fields = typeof(T).GetProperties();
            foreach (PropertyInfo field in fields) {
                SyncData meta = (SyncData)field.GetCustomAttribute(typeof(SyncData), true);
                if(meta != null) {
                    object value = field.GetValue(obj);

                    // Check the type and convert it
                    switch (Type.GetTypeCode(field.PropertyType)) {
                        case TypeCode.Boolean:
                            bytes.Add(ConvertBoolToByte((bool)value));
                            break;
                        case TypeCode.Int32:
                            bytes.AddRange(ConvertIntToByte((int)value));
                            break;
                        case TypeCode.UInt16:
                            bytes.AddRange(ConvertUShortToByte((ushort)value));
                            break;
                        case TypeCode.Single:
                            bytes.AddRange(ConvertFloatToByte((float)value)); ;
                            break;
                        case TypeCode.String:
                            if (meta.GetDataType() == SyncData.TypeOfData.ip)
                                bytes.AddRange(ConvertIpToByte((string)value));
                            else
                                bytes.AddRange(ConvertStringToByte((string)value, ref bytes));
                            break;
                        case TypeCode.Byte:
                            bytes.Add((byte)value);
                            break;
                        default:
                            switch (field.PropertyType.Name) {
                                case nameof(Vector2):
                                    bytes.AddRange(ConvertVector2ToByte((Vector2)value));
                                    break;
                                default:
                                    Debug.LogError("Type was not found and was not send : " + field.PropertyType.Name);
                                    break;
                            }
                            break;
                    }
                }
            }
            
            return bytes;
        }

        /// <summary>
        /// Convert bytes into object and apply to object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"> Instance of object which it will be applied to </param>
        /// <param name="data"> Byte data </param>
        public static void ConvertAndApplyByteToObject<T>(T obj, List<byte> data) {
            ApplyFieldsToInstance(obj, ConvertByteToObject<T>(data));
        }

        /// <summary>
        /// Convert bytes to object and get a list of the field values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"> Byte data </param>
        /// <returns></returns>
        public static List<object> ConvertByteToObject<T>(List<byte> data) {
            List<object> objectList = new List<object>();
            PropertyInfo[] fields = typeof(T).GetProperties();
            int currentIndex = 0;

            foreach (PropertyInfo field in fields) {
                SyncData meta = (SyncData)field.GetCustomAttribute(typeof(SyncData));
                if (meta != null) {
                    
                    // Check the type and convert it
                    switch (Type.GetTypeCode(field.PropertyType)) {
                        case TypeCode.Boolean:
                            objectList.Add(ConvertByteToBool(ref data, ref currentIndex));
                            break;
                        case TypeCode.Int32:
                            objectList.Add(ConvertByteToInt(ref data, ref currentIndex));
                            break;
                        case TypeCode.UInt16:
                            objectList.Add(ConvertByteToUShort(ref data, ref currentIndex));
                            break;
                        case TypeCode.Single:
                            objectList.Add(ConvertByteToFloat(ref data, ref currentIndex));
                            break;
                        case TypeCode.String:
                            if (meta.GetDataType() == SyncData.TypeOfData.ip)
                                objectList.Add(ConvertByteToIp(ref data, ref currentIndex));
                            else
                                objectList.Add(ConvertByteToString(ref data, ref currentIndex));
                            break;
                        case TypeCode.Byte:
                            currentIndex++;
                            objectList.Add(data[currentIndex - 1]);
                            break;
                        default:
                            switch (field.PropertyType.Name) {
                                case nameof(Vector2):
                                    objectList.Add(ConvertByteToVector2(ref data, ref currentIndex));
                                    break;
                            }
                            break;
                    }
                }
            }
            return objectList;
        }

        /// <summary>
        /// Loop over all properties and apply the data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"> Instance of the object which it should be applied on </param>
        /// <param name="data"> The property data which should be applied </param>
        public static void ApplyFieldsToInstance<T>(T obj, List<object> data) {
            PropertyInfo[] fields = typeof(T).GetProperties();

            int currentField = 0;
            foreach (PropertyInfo field in fields) {
                SyncData meta = (SyncData)field.GetCustomAttribute(typeof(SyncData));
                if (meta != null) {
                    field.SetValue(obj, data[currentField]);
                    currentField++;
                }

            }
        }

        #region UShort
        public static List<byte> ConvertUShortToByte(ushort num) {
            return BitConverter.GetBytes(num).ToList();
        }

        public static ushort ConvertByteToUShort(ref List<byte> data, ref int curI) {
            curI += 2;
            return BitConverter.ToUInt16(data.GetRange(curI - 2, 2).ToArray(), 0);
        }
        #endregion

        #region Int
        public static List<byte> ConvertIntToByte(int num) {
            return BitConverter.GetBytes(num).ToList();
        }

        public static int ConvertByteToInt(ref List<byte> data, ref int curI) {
            curI += 4;
            return BitConverter.ToInt32(data.GetRange(curI - 4, 4).ToArray(), 0);
        }
        #endregion

        #region Float
        public static List<byte> ConvertFloatToByte(float num) {
            return BitConverter.GetBytes(num).ToList();
        }

        public static float ConvertByteToFloat(ref List<byte> data, ref int curI) {
            curI += 4;
            return BitConverter.ToSingle(data.GetRange(curI - 4, 4).ToArray(), 0);
        }
        #endregion

        #region IP
        public static List<byte> ConvertIpToByte(string ip) {
            return IPAddress.Parse(ip).GetAddressBytes().ToList();
        }

        public static string ConvertByteToIp(ref List<byte> data, ref int curI) {
            curI += 4;
            return new IPAddress(data.GetRange(curI - 4, 4).ToArray()).ToString();
        }
        #endregion

        #region String
        public static List<byte> ConvertStringToByte(string str, ref List<byte> currentBytes) {
            currentBytes.Add((byte)str.Length);
            return Encoding.ASCII.GetBytes(str).ToList();
        }

        public static string ConvertByteToString(ref List<byte> data, ref int curI) {
            int length = data[curI];
            curI += length + 1;
            return Encoding.ASCII.GetString(data.GetRange(curI - length, length).ToArray());
        }
        #endregion

        #region Bool
        public static byte ConvertBoolToByte(bool value) {
            return BitConverter.GetBytes(value)[0];
        }

        public static bool ConvertByteToBool(ref List<byte> data, ref int curI) {
            curI++;
            return BitConverter.ToBoolean(data.GetRange(curI - 1, 1).ToArray(), 0);
        }
        #endregion

        #region Vector2
        public static List<byte> ConvertVector2ToByte(Vector2 value) {
            List<byte> byteList = new List<byte>();
            byteList.AddRange(BitConverter.GetBytes(value.x));
            byteList.AddRange(BitConverter.GetBytes(value.y));
            return byteList;
        }

        public static Vector2 ConvertByteToVector2(ref List<byte> data, ref int curI) {
            Vector2 convertedVector;
            curI += 4;
            convertedVector.x = BitConverter.ToSingle(data.GetRange(curI - 4, 4).ToArray(), 0);
            curI += 4;
            convertedVector.y = BitConverter.ToSingle(data.GetRange(curI - 4, 4).ToArray(), 0);
            return convertedVector;
        }
        #endregion
    }
}
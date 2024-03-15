using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.IO;


namespace IjhCommonUtility
{
    /// <summary>
    /// クラスとそのプロパティをファイル保存・読み込み
    /// </summary>
    public class DataContractReaderWriter
    {
        /// <summary>
        /// 書き込み（失敗時は例外発生）
        /// </summary>
        /// <param name="dataContractObject"></param>
        /// <param name="filename"></param>
        /// <param name="knownTypes">自作クラスの配列などを扱う場合は要設定</param>
        static public void WriteXml(object dataContractObject, string filename, IEnumerable<Type> knownTypes = null)
        {
            DataContractSerializer serializer = new DataContractSerializer(dataContractObject.GetType(), knownTypes);
            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                using (XmlWriter xw = XmlWriter.Create(fs, new XmlWriterSettings { Indent = true }))
                {
                    serializer.WriteObject(xw, dataContractObject);
                    xw.Flush();
                    xw.Close();
                    fs.Close();
                }
            }
        }
        /// <summary>
        /// 書き込み（失敗時はfalse）
        /// </summary>
        /// <param name="dataContractObject"></param>
        /// <param name="filename"></param>
        /// <param name="knownTypes">自作クラスの配列などを扱う場合は要設定</param>
        /// <param name="withMessageBox">ダイアログを表示する/しない</param>
        /// <returns></returns>
        static public bool WriteXml_WithoutException(object dataContractObject, string filename, IEnumerable<Type> knownTypes = null, bool withMessageBox = true)
        {
            try
            {
                WriteXml(dataContractObject, filename, knownTypes);
                return true;
            }
            catch (Exception ex)
            {
                var message = $"WriteXML Error:\n{ex.Message}";
                if (withMessageBox)
                {
                    System.Windows.MessageBox.Show(message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine(message);
                }
                return false;
            }
        }
        /// <summary>
        /// 読み込み（失敗時は例外発生）
        /// </summary>
        /// <param name="dataContractObject"></param>
        /// <param name="filename"></param>
        /// <param name="knownTypes">自作クラスの配列などを扱う場合は要設定</param>
        static public void ReadXml(object dataContractObject, string filename, IEnumerable<Type> knownTypes = null)
        {
            DataContractSerializer serializer = new DataContractSerializer(dataContractObject.GetType(), knownTypes);
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                object readObj = serializer.ReadObject(fs);
                DataContractReaderWriter.CopyDataMemberAttribute(readObj, dataContractObject);
                fs.Close();
            }
        }
        /// <summary>
        /// 読み込み（失敗時はfalse）
        /// </summary>
        /// <param name="dataContractObject"></param>
        /// <param name="filename"></param>
        /// <param name="knownTypes">自作クラスの配列などを扱う場合は要設定</param>
        /// <param name="withMessageBox">ダイアログを表示する/しない</param>
        /// <returns></returns>
        static public bool ReadXml_WithoutException(object dataContractObject, string filename, IEnumerable<Type> knownTypes = null, bool withMessageBox = true)
        {
            try
            {
                ReadXml(dataContractObject, filename, knownTypes);
                return true;
            }
            catch (Exception ex)
            {
                var message = $"ReadXML Error:\n{ex.Message}";
                if (withMessageBox)
                {
                    System.Windows.MessageBox.Show(message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine(message);
                }
                return false;
            }

        }

        static private void CopyDataMemberAttribute(object srcObj, object dstObj)
        {
            Type type = srcObj.GetType();
            MemberInfo[] members = type.GetMembers(
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static); //Remove BindingsFlags.DeclaredOnly

            foreach (MemberInfo mem in members)
            {
                if (mem.MemberType == MemberTypes.Field || mem.MemberType == MemberTypes.Property)
                {
                    DataMemberAttribute[] attribute = (DataMemberAttribute[])mem.GetCustomAttributes(typeof(DataMemberAttribute), false);
                    if (attribute.Length > 0)
                    {
                        switch (mem.MemberType)
                        {
                            case MemberTypes.Field:
                                FieldInfo fi = mem as FieldInfo;
                                object srcMemberValue = fi.GetValue(srcObj);
                                object dstMemberValue = fi.GetValue(dstObj);
                                if (dstMemberValue == null || (srcMemberValue != null && dstMemberValue != null))
                                {
                                    fi.SetValue(dstObj, srcMemberValue);
                                }
                                break;
                            case MemberTypes.Property:
                                PropertyInfo propInfo = mem as PropertyInfo;
                                if (propInfo.CanRead)
                                {
                                    object srcPropValue = propInfo.GetValue(srcObj, null);
                                    object dstPropValue = propInfo.GetValue(dstObj, null);
                                    if (dstPropValue == null || (srcPropValue != null && dstPropValue != null))
                                    {
                                        if (propInfo.CanWrite)
                                        {
                                            propInfo.SetValue(dstObj, srcPropValue, null);
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }
    }
}
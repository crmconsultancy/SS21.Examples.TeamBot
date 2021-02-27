using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS21.Examples.Integration
{
    public static class JSON
    {
        public static string Attribute<T>(string attributeName, T data, bool comma)
        {
            try
            {
                string json = "\"[attributeName]\": [value]";
                json = json.Replace("[attributeName]", attributeName);

                if (data != null)
                {
                    bool isObject = false;

                    string s = data.GetType().Name.ToLower();

                    if (isObject == false)
                    {
                        switch (data.GetType().Name.ToLower())
                        {
                            case ("string"):
                                {
                                    if (data.ToString() == string.Empty)
                                    {
                                        json = json.Replace("[value]", "null");
                                    }
                                    else
                                    {
                                        json = json.Replace("[value]", "\"" + data.ToString() + "\"");
                                    }
                                }
                                break;
                            case ("int32"):
                                {
                                    if (data.ToString() == string.Empty)
                                    {
                                        json = json.Replace("[value]", "null");
                                    }
                                    else if (data.ToString() == "0")
                                    {
                                        json = json.Replace("[value]", "null");
                                    }
                                    else
                                    {
                                        json = json.Replace("[value]", data.ToString());
                                    }
                                }
                                break;
                            case ("double"):
                                {
                                    if (data.ToString() == string.Empty)
                                    {
                                        json = json.Replace("[value]", "null");
                                    }
                                    else if (data.ToString() == "0" || data.ToString() == "0.0")
                                    {
                                        json = json.Replace("[value]", "null");
                                    }
                                    else
                                    {
                                        json = json.Replace("[value]", data.ToString());
                                    }
                                }
                                break;
                            case ("datetime"):
                                {
                                    if (data.ToString() == string.Empty)
                                    {
                                        json = json.Replace("[value]", "null");
                                    }
                                    else
                                    {
                                        DateTime dt = Convert.ToDateTime(data);
                                        json = json.Replace("[value]", "\"" + dt.ToString("dd/MM/yyyy") + "\"");
                                    }
                                    break;
                                }
                            case ("boolean"):
                                {
                                    if (data.ToString() == string.Empty)
                                    {
                                        json = json.Replace("[value]", "null");
                                    }
                                    else
                                    {
                                        json = json.Replace("[value]", data.ToString().ToLower());
                                    }
                                    break;
                                }
                            default:
                                {
                                    // Enum check
                                    bool isEnum = data is Enum;

                                    if (isEnum)
                                    {
                                        // handle the enum as an Int
                                        string s1 = data.ToString();

                                        if (data.ToString() == string.Empty)
                                        {
                                            json = json.Replace("[value]", "null");
                                        }
                                        else
                                        {
                                            //int intVal = Convert.ToInt32(data);

                                            json = json.Replace("[value]", "\"" + s1 + "\"");
                                        }
                                    }
                                    else
                                    {
                                        if (data.ToString() == string.Empty)
                                        {
                                            json = json.Replace("[value]", "null");
                                        }
                                        else
                                        {
                                            json = json.Replace("[value]", "\"" + data.ToString() + "\"");
                                        }
                                    }

                                    break;
                                }
                        }
                    }
                }
                else
                {
                    json = json.Replace("[value]", "null");
                }

                if (comma)
                {
                    json += ",";
                }

                return json;
            }
            catch (Exception e)
            {
                throw new IntegrationException(e.Message, "JSON", "Attribute", IntegrationException.ExceptionType.Internal);
            }
        }
        public static bool TryCast<T>(this object obj, out T result)
        {
            if (obj is T)
            {
                result = (T)obj;
                return true;
            }

            result = default(T);
            return false;
        }
    }
}



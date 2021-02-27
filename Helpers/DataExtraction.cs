using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.ServiceModel.Description;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SS21.Examples.TeamBot
{
    public static class DataExtraction
    {
        public static string ExtractUser(this string value)
        {
            string username = string.Empty;

            return username;
        }
        public static string ExtractGUID(this string value)
        {
            var match = Regex.Match(value, @"([a-z0-9]{8}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{12})");
            if (match.Success)
            {
                return match.Value;
            }
            return null;
        }

        public static string ConvertMentionsToUrls(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }
            string output = message;

            // find all mentions by [ ] characters
            string[] matchResults = Regex.Matches(message, @"\[(.+?)\]")
                            .Cast<Match>()
                            .Select(s => s.Groups[1].Value).ToArray();

            foreach (string mention in matchResults)
            {
                string url = ReturnMentionToUrl(mention);
                output = output.Replace("@[" + mention + "]", url);
            }
            return output;
        }

        public static string ReturnMentionToUrl(string mention)
        {
            // expected input format - @[1,80E70E6B-CB2B-EA11-A810-002248070F4C,"Nuffield Trust"]
            // expected output format - <a href=”/main.aspx?etc=1&id=80E70E6B-CB2B-EA11-A810-002248070F4C&pagetype=entityrecord”>Nuttfield Trust</a>
            if (string.IsNullOrEmpty(mention))
            {
                return string.Empty;
            }

            string input = mention;
            string output = "<a href=”/main.aspx?etc={typecode}&id={recordid}&pagetype=entityrecord”>{recordname}</a>";

            // remove specific symbols
            input = input.Replace("@", "");
            input = input.Replace("[", "");
            input = input.Replace("]", "");

            // split by comma
            string[] inputValues = input.Split(',');

            int typeCode = 0;
            Guid recordId = Guid.Empty;
            string recordName = string.Empty;

            foreach (string value in inputValues)
            {
                if (typeCode == 0)
                {
                    int.TryParse(value, out typeCode);
                }
                if (recordId == Guid.Empty)
                {
                    Guid.TryParse(value, out recordId);
                }
                if (recordName == string.Empty)
                {
                    if (value.Contains('"') || value.Contains("\'"))
                    {
                        recordName = value.Trim('"');
                        recordName = value.Replace("'", "");
                        recordName = value.Replace("\"", "");

                    }
                }
            }
            // replace slugs in final output
            output = output.Replace("{typecode}", typeCode.ToString());
            output = output.Replace("{recordid}", recordId.ToString());
            output = output.Replace("{recordname}", recordName);

            return output;
        }

        public static string ExtractPreviousWord(this string value, string criteria)
        {
            string[] values = value.Split(" ");

            for (int i = 0; i < values.Length; i++)
            {
                string currValue = values[i];
                if (currValue.ToLower().Contains(criteria.ToLower()))
                {
                    return values[--i];
                }
            }
            return string.Empty;
        }
        public static DateTime ExtractDate(this string value)
        {
            DateTime date = DateTime.MinValue;

            if (value != string.Empty)
            {
                bool parseSuccessful = false;

                CultureInfo culture = CultureInfo.CreateSpecificCulture("en-gb");
                DateTime.TryParseExact(value, "dd/MM/yyyy", culture, DateTimeStyles.None, out date);
                // DateTime.TryParse(value, out date);

                if (date != DateTime.MinValue && date != DateTime.MaxValue)
                {
                    parseSuccessful = true;
                }

                // if the direct string to date parse has failed 
                if (parseSuccessful == false)
                {
                    // perform regex to extract date from the wider string
                    string dateSng = string.Empty;

                    // Improvement - code reduction, loop over checks
                    List<string[]> dateChecks = new List<string[]>();
                    dateChecks.Add(new string[] { @"\d{2}\/\d{2}\/\d{4}" });
                    dateChecks.Add(new string[] { @"\d{2}\-\d{2}\-\d{4}" });
                    dateChecks.Add(new string[] { @"\d{2}\/\d{2}\/\d{2}" });
                    dateChecks.Add(new string[] { @"\d{2}\-\d{2}\-\d{2}" });
                    dateChecks.Add(new string[] { @"\d{2}\-\d{2}" });
                    dateChecks.Add(new string[] { @"\d{2}\/\d{2}" });

                    foreach (string[] dateCheck in dateChecks)
                    {
                        if (string.IsNullOrEmpty(dateSng))
                        {
                            string regexMask = dateCheck[0];
                            string dateFormat = "dd/MM/yyyy";

                            if (dateCheck.Length == 2)
                            {
                                dateFormat = dateCheck[1];
                            }
                            dateSng = ExtractDateStringBasedOnRegexMask(value, regexMask, dateFormat);
                        }
                        else
                        {
                            // date found, break
                            break;
                        }
                    }

                    // CultureInfo culture = CultureInfo.CreateSpecificCulture("en-gb");
                    DateTime.TryParseExact(dateSng, "dd/MM/yyyy", culture, DateTimeStyles.None, out date);
                }
            }
            return date;
        }
        private static string ExtractDateStringBasedOnRegexMask(string value, string regexMask, string dateFormat = "dd/MM/yyyy")
        {
            try
            {
                DateTime date;

                Regex dateTime = new Regex(regexMask);
                Match m = dateTime.Match(value);

                string result = m.Value;

                if (result.Length == 5)
                {
                    result += "/" + DateTime.Now.Year;
                }

                date = DateTime.ParseExact(result, dateFormat, null);

                if (date != DateTime.MinValue && date != DateTime.MaxValue)
                {
                    return date.ToString("dd/MM/yyyy");
                }
            }
            catch (Exception e)
            {
                // a parsing error has occured, not need to raise the exception here as other date string tests are performed
            }
            return string.Empty;
        }
        public static string ExtractTelephoneNumber(this string value)
        {
            // Rules and logic for what a Telephone Number is!    
            string output = string.Empty;

            if (value != string.Empty)
            {
                string telephoneString = string.Empty;
                // regex pattern to extract telephone
                Regex phoneNumberRegex = new Regex(@"\+?(\d[\d-. ]+)?(\([\d-. ]+\))?[\d-. ]+\d");
                Match m = phoneNumberRegex.Match(value);

                while (m.Success)
                {
                    // build telephone string
                    telephoneString = m.Value;
                    m = m.NextMatch();
                }

                if (!string.IsNullOrEmpty(telephoneString))
                {
                    // if the telephoneString meets the minimum required characters to be a telephone number
                    char[] telephoneCharacters = telephoneString.ToCharArray();

                    int noOfDigits = 0;

                    foreach (char c in telephoneCharacters)
                    {
                        if (char.IsDigit(c))
                        {
                            noOfDigits++;
                        }
                    }

                    if (noOfDigits >= 11)
                    {
                        // map telephone string to output.
                        output = telephoneString;
                    }
                }
            }
            return output;
        }
        public static string ToCommaSeparatedString(this Microsoft.Xrm.Sdk.EntityCollection value)
        {
            if(value == null)
            {
                return "None";
            }

            string output = string.Empty;
            foreach (Microsoft.Xrm.Sdk.Entity record in value.Entities)
            {
                if (record.Contains("partyid"))
                {
                    if (!(output.Equals(String.Empty)))
                    {
                        output += ", ";
                    }

                    Microsoft.Xrm.Sdk.EntityReference entRef = (Microsoft.Xrm.Sdk.EntityReference)record.Attributes["partyid"];
                    output += entRef.Name + "";
                }
                else if (record.Contains("addressused"))
                {
                    string emailAddress = (string)record["addressused"];
                    if (!(emailAddress.Equals(String.Empty)))
                    {
                        if (!(output.Equals(String.Empty)))
                        {
                            output += ", ";
                        }
                        output += emailAddress;
                    }
                }
                else if (record.Contains("name"))
                {
                    string name = (string)record["name"];
                    if (!(name.Equals(String.Empty)))
                    {
                        if (!(output.Equals(String.Empty)))
                        {
                            output += ", ";
                        }
                        output += name;
                    }
                }
                else if (record.Contains("crmcs_name"))
                {
                    string name = (string)record["crmcs_name"];
                    if (!(name.Equals(String.Empty)))
                    {
                        if (!(output.Equals(String.Empty)))
                        {
                            output += ", ";
                        }
                        output += name;
                    }
                }
            }
            return output;
        }
    }
}

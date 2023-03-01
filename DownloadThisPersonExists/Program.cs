using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Net.Http;
using NetVips;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace DownloadThisPersonExists
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\cacheimg");
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\images");
                Random rd = new Random();
                //int min = 0;
                //int max = 0;
                //Console.Write("Bat dau download tu anh so:");
                //min = Int32.Parse(Console.ReadLine());
                //Console.Write("den so:");
                //max = Int32.Parse(Console.ReadLine());

                int slanh;
                Console.Write("Nhap so luong anh muon download:");
                slanh = Int32.Parse(Console.ReadLine());

                Console.WriteLine("Chuan bi download");
                for (int i = 0; i <= slanh; i++)
                {
                    int index = rd.Next(0, 65000);
                    Task<string> result = GetThisPersonExitsImageLink(index);
                    dynamic resjson = JObject.Parse(result.Result);
                    string imglink = Convert.ToString(resjson.photo_url);
                    string facequad = Convert.ToString(resjson.face_quad);
                    facequad = facequad.Replace("\r\n", string.Empty);
                    facequad = facequad.Replace("[", string.Empty);
                    facequad = facequad.Replace("]", string.Empty);
                    facequad = facequad.Replace(" ", string.Empty);
                    var facequadsplit = facequad.Split(',');
                    using (WebClient client = new WebClient())
                    {
                        try
                        {
                            client.DownloadFile(new Uri(imglink), @"cacheimg\\" + index.ToString() + ".jpg");

                            Image image = Image.NewFromFile("cacheimg\\" + index.ToString() + ".jpg");
                            string outFilename = "images\\" + index.ToString() + ".jpg";

                            int topLeftX = int.Parse(facequadsplit[0].Split('.')[0]);
                            int topLeftY = int.Parse(facequadsplit[1].Split('.')[0]);
                            int topRightX = int.Parse(facequadsplit[2].Split('.')[0]);
                            int topRightY = int.Parse(facequadsplit[3].Split('.')[0]);
                            int bottomRightX = int.Parse(facequadsplit[4].Split('.')[0]);
                            int bottomRightY = int.Parse(facequadsplit[5].Split('.')[0]);
                            int bottomLeftX = int.Parse(facequadsplit[6].Split('.')[0]);
                            int bottomLeftY = int.Parse(facequadsplit[7].Split('.')[0]);

                            // the angle the top edge is rotated by
                            int dx = topRightX - topLeftX;
                            int dy = topRightY - topLeftY;
                            double angle = (180 / Math.PI) * Math.Atan2(dx, dy);
                            if (angle < -45 || angle >= 45)
                            {
                                angle = 90 - angle;
                            }

                            // therefore the angle to rotate by to get it straight
                            //angle = -angle;

                            //double angle = -Math.Atan2(bottomLeftY - topLeftY, bottomLeftX - topLeftX);

                            image = image.Rotate(angle);

                            // the new position of the rectangle in the rotated image
                            double radians = (Math.PI * angle) / 180.0;
                            double c = Math.Cos(radians);
                            double s = Math.Sin(radians);

                            int left = Convert.ToInt32(topLeftX * c - topLeftY * s);
                            int top = Convert.ToInt32(topLeftX * s + topLeftY * c);
                            int width = Convert.ToInt32(Math.Sqrt(Math.Pow(topRightX - topLeftX, 2) +
                                                                  Math.Pow(topRightY - topLeftY, 2)));
                            int height = Convert.ToInt32(Math.Sqrt(Math.Pow(topRightX - bottomRightX, 2) +
                                                                   Math.Pow(topRightY - bottomRightY, 2)));

                            // after a rotate, the new position of the origin is given by .Xoffset, .Yoffset
                            Image tile = image.Crop(left + image.Xoffset, top + image.Yoffset, width, height);

                            tile.WriteToFile(outFilename);

                            //File.Delete("cacheimg\\" + index.ToString() + ".jpg");

                            Console.WriteLine("Tai xong anh thu " + (i+1).ToString() + " (" + index.ToString() + ")");
                        }
                        catch (Exception exz)
                        {
                            if(exz.Message.Contains("The remote server returned an error"))
                            {
                                
                            }
                            else
                            {
                                throw new Exception(exz.Message);
                            }
                        }
                        
                        //client.DownloadFileAsync(new Uri("https://thispersondoesnotexist.com/image"), @"thispersondoesnotexist.jpg");
                    }
                    //FileInfo a = new FileInfo(@"images\\" + i.ToString() + ".jpg");
                    //if (a.Length < 50)
                    //{
                    //    File.Delete(@"images\\" + i.ToString() + ".jpg");
                    //}
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Loi " + ex.Message);
            }
        }

        async static Task<string> GetThisPersonExitsImageLink(int rd)
        {
            var httpClient = new HttpClient();

            var response = await httpClient.GetAsync("https://thispersonexists.net/data/" + rd.ToString() + ".json");
            var contents = await response.Content.ReadAsStringAsync();

            return contents;
        }
    }
}

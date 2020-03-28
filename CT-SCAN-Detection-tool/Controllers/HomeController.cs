using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CT_SCAN_Detection_tool.Models;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using CT_SCAN_Detection_tool.Options;
using System.Net.Http;
using System.IO;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Drawing;

namespace CT_SCAN_Detection_tool.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IOptions<PredictionAPI> _config;
        private readonly IHostingEnvironment _hostingEnvironment;

        public HomeController(ILogger<HomeController> logger, IOptions<PredictionAPI> config, IHostingEnvironment hostingEnvironment)
        {
            _logger = logger;
            _config = config;
            _hostingEnvironment = hostingEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> MakePredictionRequest(HomeViewModel model)
        {
            if (model.AttachedFile == null)
                return View("Index", model);



            var client = new HttpClient();

            client.DefaultRequestHeaders.Add("Prediction-Key", _config.Value.Key);

            string url = _config.Value.Url;

            HttpResponseMessage response;

            var combinedPath = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
            var fileName = "org" + DateTime.Now.ToString("yyyyMMddhhmmss") + model.AttachedFile.FileName;
            var fileNameNew = DateTime.Now.ToString("yyyyMMddhhmmss") + model.AttachedFile.FileName;
            var filePath = Path.Combine(combinedPath, fileName);
            var filePathNew = Path.Combine(combinedPath, fileNameNew);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                model.AttachedFile.CopyTo(stream);
            }

            byte[] byteData = GetImageAsByteArray(filePath);

            JsonObj result = null;

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(url, content);
                var res = await response.Content.ReadAsStringAsync();

                result = JsonConvert.DeserializeObject<JsonObj>(res);
            }

            var probability = result.Predictions.Where(x => x.TagName == "Pneumonia").Select(x => x.Probability).FirstOrDefault();

            if (probability > _config.Value.Probability)
            {
                await SecondScan(filePath, filePathNew);
                model.Result = $"<p class=\"text-danger info\">Additional patient health verification required </p>";
                model.Src = $"/temp/{fileNameNew}";
                model.Class = "zdjecie";
            }
            else
            {
                model.Src = $"/temp/{fileName}";
                model.Class = "";
            }

            model.Procent = Math.Round(((decimal)probability * 100m) / 1m, 2);

            return View("Index", model);
        }

        private async Task SecondScan(string filePath, string filePathNew)
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Add("Prediction-Key", _config.Value.Key);

            string url = _config.Value.Url2;

            HttpResponseMessage response;

            byte[] byteData = GetImageAsByteArray(filePath);

            JsonObj2 result = null;

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(url, content);
                var res = await response.Content.ReadAsStringAsync();

                result = JsonConvert.DeserializeObject<JsonObj2>(res);
            }

            Bitmap originalBmp = (Bitmap)Image.FromFile(filePath);

            Bitmap tempBitmap = new Bitmap(originalBmp, originalBmp.Width, originalBmp.Height);

            using (Graphics g = Graphics.FromImage(tempBitmap))
            {
                var photoW = originalBmp.Width;
                var photoH = originalBmp.Height;
                Pen redPen = new Pen(Color.Red, 3);
                var points = result.predictions.ToList();
                var sort = points.OrderByDescending(x => x.probability).ToList();
                int i = 1;
                //foreach (var point in points.Where(x => x.probability > _config.Value.Probability2).ToList())
                foreach (var point in sort)
                {
                    int x = Convert.ToInt32(photoW * point.boundingBox.left);
                    int y = Convert.ToInt32(photoH * point.boundingBox.top);
                    int w = Convert.ToInt32(photoW * point.boundingBox.width);
                    int h = Convert.ToInt32(photoH * point.boundingBox.height);
                    Rectangle rect = new Rectangle(x, y, w, y);
                    g.DrawRectangle(redPen, rect);

                    if (i >= 5)
                        break;

                    i++;
                }
            }

            Image image = (Image)tempBitmap;
            image.Save(filePathNew);
        }

        private static byte[] GetImageAsByteArray(string imageFilePath)
        {
            FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }
    }

    public class JsonObj
    {
        public string Id { get; set; }
        public string Project { get; set; }
        public string Iteration { get; set; }
        public string Created { get; set; }
        public List<JsonObjItem> Predictions { get; set; }
    }

    public class JsonObjItem
    {
        public string TagId { get; set; }
        public string TagName { get; set; }
        public float Probability { get; set; }
    }

    public class JsonObj2
    {
        public string id { get; set; }
        public string project { get; set; }
        public string iteration { get; set; }
        public string created { get; set; }
        public List<JsonPoints> predictions { get; set; }
    }

    public class JsonPoints
    {
        public float probability { get; set; }
        public string tagId { get; set; }
        public string tagName { get; set; }
        public JsonPoint boundingBox { get; set; }
    }

    public class JsonPoint
    {
        public float left { get; set; }
        public float top { get; set; }
        public float width { get; set; }
        public float height { get; set; }
    }
}

using LovelyFish.API.Data;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1;
using System;
using System.Text;
using System.Xml.Linq;

namespace LovelyFishAquarium.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SitemapController : ControllerBase
    {
        private readonly LovelyFishContext _context; //Db object
        public SitemapController(LovelyFishContext context) => _context = context; //inject Db

        [HttpGet]
        [Route("/sitemap.xml")]
        public IActionResult GetSitemap() //function GetSitemap(), will return JSON, XML, HTML, FILE.
        {
            var baseUrl = "https://www.lovelyfishaquarium.co.nz";

            var products = _context.Products.Select(p => p.Id).ToList(); // use ID

            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var urlset = new XElement(ns + "urlset");

            // main page
            urlset.Add(new XElement(ns + "url",
                        new XElement(ns + "loc", baseUrl + "/"),
                        new XElement(ns + "changefreq", "daily"),
                        new XElement(ns + "priority", "1.0")));

            // static page
            var staticPages = new[] { "about", "contact" };
            foreach (var page in staticPages)
            {
                urlset.Add(new XElement(ns + "url",
                            new XElement(ns + "loc", $"{baseUrl}/{page}"),
                            new XElement(ns + "changefreq", "monthly"),
                            new XElement(ns + "priority", "0.5")));
            }

            // product page
            foreach (var id in products)
            {
                urlset.Add(new XElement(ns + "url",
                            new XElement(ns + "loc", $"{baseUrl}/product/{id}"),
                            new XElement(ns + "changefreq", "weekly"),
                            new XElement(ns + "priority", "0.8")));
            }

            var xml = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), urlset);
            return Content(xml.ToString(), "application/xml", Encoding.UTF8);
        }
    }
}


//< urlset >
//   < url >
//      < loc > ...</ loc >
//   </ url >
//</ urlset >
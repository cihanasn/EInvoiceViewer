using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace EInvoiceViewer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                string folderPath = textBox1.Text.Trim();

                string xmlPath = folderPath;

                if (!File.Exists(xmlPath))
                {
                    MessageBox.Show("XML dosyası bulunamadı.");
                    return;
                }

                XDocument xdoc = XDocument.Load(xmlPath);

                var base64Xslt = xdoc.Descendants()
                        .Where(e => e.Name.LocalName == "EmbeddedDocumentBinaryObject")
                        .FirstOrDefault()?.Value;

                if (string.IsNullOrEmpty(base64Xslt))
                {
                    MessageBox.Show("XSLT bulunamadı.");
                    return;
                }

                byte[] xsltBytes = Convert.FromBase64String(base64Xslt);
                string xsltContent = Encoding.UTF8.GetString(xsltBytes);

                // Geçici xslt dosyası oluştur
                string tempXsltPath = Path.GetTempFileName();
                File.WriteAllText(tempXsltPath, xsltContent);

                // XmlDocument olarak yükle
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlPath);

                // XSLT uygula
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(tempXsltPath);

                // Bellekte HTML çıktısı almak için yazıcı (writer) hazırlanıyor. Yani çıktı dosyaya değil, hafızaya yazılır.
                using var sw = new StringWriter(); // → HTML'yi string olarak almak için

                using var writer = XmlWriter.Create(sw, xslt.OutputSettings);
                // → xslt.OutputSettings: XSLT içindeki <xsl:output> ayarlarını uygular. Örnek : <xsl:output method="html" indent="yes"/>

                // XML + XSLT → HTML dönüşümü yapılıyor
                xslt.Transform(xmlDoc, null, writer);
                /*
                    xmlDoc: XML veri kaynağı

                    null: XSLT parametreleri kullanılmayacaksa null

                    writer: çıktı bu yazıcıya (yani sw içine) yazılır
                */

                // Sadece ilk 100 karakterini mesaj olarak göster
                string htmlPreview = sw.ToString();

                string tempFile = Path.Combine(Path.GetTempPath(), "fatura_preview.html");
                File.WriteAllText(tempFile, htmlPreview);
                System.Diagnostics.Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });

                //MessageBox.Show("HTML çıktısı:\n" + htmlPreview.Substring(0, Math.Min(500, htmlPreview.Length)));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "XML Dosyaları (*.xml)|*.xml";
            ofd.Title = "Bir XML fatura dosyası seçin";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = ofd.FileName;
            }
        }
    }
}

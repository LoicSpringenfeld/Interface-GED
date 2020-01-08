using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Windows.Forms;
using System.Threading;

namespace Interface_GED
{
    class Program
    {
        static HttpClient client = new HttpClient();
        static string token = "";

        static string[] GetConnection()
        {
            string[] login = new string[2];
            Console.Write("Username : ");
            login[0] = Console.ReadLine();
            Console.Write("Password : ");
            login[1] = Console.ReadLine();
            return login;
        }

        static async Task<string> PostTokenAsync()
        {
            Boolean isSuccessStatusCode = false;
            string token = "";
            while (isSuccessStatusCode == false)
            {
                string[] login = GetConnection();
                var request = new HttpRequestMessage(HttpMethod.Post, "/token");
                var requestContent = string.Format("grant_type=password&username={0}&password={1}", Uri.EscapeDataString(login[0]), Uri.EscapeDataString(login[1]));
                request.Content = new StringContent(requestContent, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");
                var response = await client.SendAsync(request);
                var content = response.Content.ReadAsStringAsync().Result;
                if (response.IsSuccessStatusCode)
                {
                    isSuccessStatusCode = true;
                    TokenJson tj = JsonConvert.DeserializeObject<TokenJson>(content);
                    token = tj.access_token;
                    Console.Clear();
                    Console.WriteLine("Connexion réussie");
                }
                else
                {
                    Console.Clear();
                    Console.WriteLine("Connexion impossible. Veuillez à nouveau saisir votre mot de passe et votre nom d'utilisateur.");
                }
            }
            return token;
        }

        static async Task<bool> OpenDocument()
        {
            Console.Write("Saisir l'id du document à effectuer : ");
            var id = Console.ReadLine();
            await GetDocumentAsync(id);
            await GetMetadataAsync(id);
            Console.Write("Voulez-vous ouvrir un autre document ? (O/N) : ");
            var ouvrir = Console.ReadLine();
            if (ouvrir.ToUpper() == "O")
            {
                Console.Clear();
                return true;
            } else
            {
                return false;
            }
        }

        static async Task GetDocumentAsync(string id)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync($"/api/document/{id}/display");
            var content = response.Content.ReadAsStringAsync().Result;
            if (response.IsSuccessStatusCode)
            {
                DocumentJson dj = JsonConvert.DeserializeObject<DocumentJson>(content);
                Console.Clear();
                Console.WriteLine($"Ouverture du document {id} : {dj.FileName}");
                try
                {
                    byte[] bytesFile = Convert.FromBase64String(dj.File);
                    FileStream stream = new FileStream($@"D:\{dj.FileName}", FileMode.CreateNew);
                    BinaryWriter writer = new BinaryWriter(stream);
                    writer.Write(bytesFile, 0, bytesFile.Length);
                    writer.Close();
                    System.Diagnostics.Process.Start($@"D:\{dj.FileName}");
                } catch (IOException)
                {
                    System.Diagnostics.Process.Start($@"D:\{dj.FileName}");
                }
            }
        }

        static async Task GetMetadataAsync(string id)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync($"/api/document/{id}/metadata");
            var content = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine($"Métadonnées du document {id} :");
            Console.WriteLine(content);
            Console.Write("Voulez-vous copier les métadonnées ? (O/N) : ");
            var copier = Console.ReadLine();
            if (copier.ToUpper() == "O")
            {
                Thread thread = new Thread(() => Clipboard.SetText(content));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
                Console.WriteLine("Métadonnées copiées dans le clipboard !");
            }
        }

        static async Task RunAsync()
        {
            client.BaseAddress = new Uri("http://157.26.82.44:2240");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            try
            {
                token = await PostTokenAsync();
                var openDocument = true;
                while(openDocument)
                {
                    openDocument = await OpenDocument();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void Main(string[] args)
        {
            RunAsync().GetAwaiter().GetResult();

        }
    }
}

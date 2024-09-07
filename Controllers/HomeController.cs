using System.Diagnostics;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using twitter_clone.Models;
using System.Net;
using System.Net.Mail;

namespace twitter_clone.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly Supabase.Client _supabaseClient;

    private readonly IWebHostEnvironment _hostingEnvironment;

    public HomeController(ILogger<HomeController> logger, Supabase.Client supabaseClient, IWebHostEnvironment hostingEnvironment)
    {
        _logger = logger;
        _supabaseClient = supabaseClient;
        _hostingEnvironment = hostingEnvironment;
    }

    // GET: Home/
    // Inicialización de los ViewBags
    public async Task<IActionResult> Index()
    {
        // Últimos usuarios
        var latestUsersResponse = await _supabaseClient
            .From<UsersModel>()
            .Select("*")
            .Order(u => u.User_created_at, Supabase.Postgrest.Constants.Ordering.Descending)
            .Limit(4)
            .Get();

        var latestUsers = latestUsersResponse.Models;

        ViewBag.LatestUsers = latestUsers;
        
        var username = Request.Cookies["UserName"];

        if (username == null)
        {
            return RedirectToAction("Login", "Users");
        }
        ViewBag.UserName = username;

        var response = await _supabaseClient.From<UsersModel>().Where(u => u.User_username == username).Get();

        if (response == null)
        {
            return RedirectToAction("Login", "Users");
        }

        var _user = response.Models.FirstOrDefault();

        if (_user == null)
        {
            return RedirectToAction("Login", "Users");
        }
        
        ViewBag.SelfPicture = _user.User_profile_picture_path;

        ViewBag.UserFirstName = _user.User_first_name;

        ViewBag.PicturePath = _user.User_profile_picture_path;

        var follows_response = await _supabaseClient.From<FollowsModel>().Where(x => x.Follow_follower_id == _user.User_id).Get();

        if (follows_response == null)
        {
            return RedirectToAction("Index", "Users");
        }

        var _follows = follows_response.Models;

        ViewBag._tweets = new List<TweetsModel>();
        ViewBag._tweets_users = new List<UsersModel>();
        ViewBag._retweets = new List<TweetsModel>();
        ViewBag._retweets_user = new List<UsersModel>();
        ViewBag._like = new List<int>();
        ViewBag._cont = 0;
        ViewBag._cont_re = 0;

        foreach (var item in _follows)
        {
            var tweets_response = await _supabaseClient
                                    .From<TweetsModel>()
                                    .Where(x => x.Tweet_user_id == item.Follow_followed_id)
                                    .Order("tweet_created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                                    .Get();

            if (tweets_response != null)
            {
                var _tweets_item = tweets_response.Models;

                foreach (var singleTweet in _tweets_item)
                {
                    // Para cada tweet se debe encontrar a su respectivo usuario

                    var _followed_response = await _supabaseClient.From<UsersModel>().Where(x => x.User_id == singleTweet.Tweet_user_id).Get();

                    if (_followed_response == null)
                    {
                        return RedirectToAction("Index", "Users");
                    }

                    var _followed = _followed_response.Models.FirstOrDefault();

                    if (_followed == null)
                    {
                        return RedirectToAction("Index", "Users");
                    }

                    // Para cada tweet se debe determinar si el usuario ha dejado su like

                    var _like = await _supabaseClient.From<LikesModel>().Where(x => x.Like_tweet_id == singleTweet.Tweet_id).Single();

                    if (_like == null)
                    {
                        // No se encontró un like
                        ViewBag._like.Add(0);
                    }
                    else
                    {
                        ViewBag._like.Add(1);
                    }

                    // Revisamos si el tweet es un retweet

                    if (singleTweet.Tweet_re_id != null)
                    {
                        // Es un retweet

                        var _re_response = await _supabaseClient.From<RetweetsModel>().Where(x => x.Retweet_id == singleTweet.Tweet_re_id).Single();

                        if (_re_response != null)
                        {
                            // Accedo al tweet original

                            var _retweet_response = await _supabaseClient.From<TweetsModel>().Where(x => x.Tweet_id == _re_response.Retweet_original_id).Single();

                            if (_retweet_response != null)
                            {
                                // Accedo al usuario del tweet original

                                var _retweet_user = await _supabaseClient.From<UsersModel>().Where(x => x.User_id == _retweet_response.Tweet_user_id).Single();

                                if (_retweet_user != null)
                                {
                                    ViewBag._retweets_user.Add(_retweet_user);

                                    ViewBag._retweets.Add(_retweet_response);

                                    ViewBag._tweets.Add(singleTweet);

                                    ViewBag._tweets_users.Add(_followed);
                                }
                            }
                        }
                    }
                    else
                    {
                        ViewBag._tweets.Add(singleTweet);

                        ViewBag._tweets_users.Add(_followed);
                    }
                }
            }
        }
        return View();
    }

    // POST: Home/Tweet
    [HttpPost]
    public async Task<IActionResult> Tweet(TweetingModel model)
    {
        var userId = Request.Cookies["UserID"];
        if (userId == null)
        {
            Console.WriteLine("UserId not available.");
            return RedirectToAction("Index", "Users");
        }
        TweetsModel tweet = new()
        {
            Tweet_user_id = Guid.Parse(userId),
            Tweet_content = model.Tweet_content,
            Tweet_re_id = model.Tweet_re_id,
            Tweet_created_at = DateTimeOffset.Now
        };

        // Añadir imagen del tweet

        if (model.Tweet_image != null)
        {
            Console.WriteLine(model.Tweet_image.FileName);
            string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "assets");
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.Tweet_image.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.Tweet_image.CopyToAsync(fileStream);
            }
            tweet.Tweet_picture_path = "~/assets/" + uniqueFileName;
        }

        var response = await _supabaseClient.From<TweetsModel>().Insert(tweet);

        if (response.Models == null || response.Models.Count == 0)
        {
            Console.WriteLine("Database crash.");
            return RedirectToAction("Index", "Users");
        }

        var users = await _supabaseClient.From<UsersModel>()
                                        .Select("*")
                                        .Get();

        var _users = users.Models;

        if (_users != null)
        {
            Console.WriteLine("Encuentra usuarios.");
        }

        foreach (var item in _users)
        {
            if (tweet.Tweet_content.Contains(item.User_username))
            {
                Console.WriteLine("Encuentra mención");
                const string subject = "Tienes una nueva mención.";
                string body = "El usuario "+Request.Cookies["UserName"]+" te ha mencionado en un tweet.";

                send(item.User_email, item.User_first_name, body, subject);
                break;
            }
        }

        return RedirectToAction(nameof(Index));
    }

    public void send(string destinatario, string name, string mensaje, string titulo)
    {
        var fromAddress = new MailAddress("unettwitter@gmail.com", "From Twitter");
        var toAddress = new MailAddress(destinatario, "To " + name);
        const string fromPassword = "faonexdljihjdknd";
        string subject = titulo;
        string body = mensaje;


        // Configuración del cliente SMTP
        var smtp = new SmtpClient
        {
            Host = "smtp.gmail.com",
            Port = 587,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
        };

        // Crear y enviar el correo
        using (var message = new MailMessage(fromAddress, toAddress)
        {
            Subject = subject,
            Body = body
        })
        {
            smtp.Send(message);
        }
    }

    // POST: Home/Like
    [HttpPost]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> Like([FromBody] LikeRequest request)
    {
        var tweetId = request.TweetId;
        var userId = Request.Cookies["UserID"];

        // Verificar si el tweet existe
        var tweet = await _supabaseClient.From<TweetsModel>()
                                         .Select("*")
                                         .Where(x => x.Tweet_id == tweetId)
                                         .Single();

        if (tweet == null)
        {
            return NotFound();
        }

        Guid parsedUserId;
        if (!Guid.TryParse(userId, out parsedUserId))
        {
            return BadRequest("User ID inválido.");
        }

        // Verificar si el usuario ya dio like a este tweet
        var existingLike = await _supabaseClient.From<LikesModel>()
                                                .Select("*")
                                                .Where(u => u.Like_user_id == parsedUserId && u.Like_tweet_id == tweet.Tweet_id)
                                                .Single();


        if (existingLike != null)
        {
            // Si ya dio like, eliminarlo y reducir el contador de likes
            tweet.Tweet_likes_quantity -= 1;

            await _supabaseClient
                .From<LikesModel>()
                .Where(u => u.Like_user_id == parsedUserId && u.Like_tweet_id == tweet.Tweet_id)
                .Delete();


            var result = await _supabaseClient.From<TweetsModel>()
                                              .Upsert(tweet);

            if (result != null)
            {
                return Json(new { newLikeCount = tweet.Tweet_likes_quantity });
            }
        }
        else
        {
            tweet.Tweet_likes_quantity += 1;

            var result = await _supabaseClient.From<TweetsModel>()
                                              .Upsert(tweet);

            if (result != null)
            {
                LikesModel model = new()
                {
                    Like_user_id = Guid.Parse(userId),
                    Like_tweet_id = tweetId
                };

                await _supabaseClient.From<LikesModel>()
                                     .Insert(model);

                return Json(new { newLikeCount = tweet.Tweet_likes_quantity });
            }
        }

        return StatusCode(500, "Error al actualizar el tweet");
    }

    // POST: Home/Retweet
    [HttpPost]
    public async Task<IActionResult> Retweet([FromBody] RetweetRequest request)
    {
        var tweetId = request.TweetId;
        var userId = Request.Cookies["UserID"];

        // Verificaciones primarias

        var tweet = await _supabaseClient.From<TweetsModel>()
                                         .Select("*")
                                         .Where(x => x.Tweet_id == tweetId)
                                         .Single();
        if (tweet == null)
        {
            return NotFound();
        }

        if (!Guid.TryParse(userId, out Guid parsedUserId))
        {
            return BadRequest("User ID inválido.");
        }

        Console.WriteLine("Pasa verificaciones primarias.");

        // Creación del modelo de Retweet

        RetweetsModel retweetModel = new()
        {
            Retweet_original_id = tweet.Tweet_id,
            Retweet_user = parsedUserId,
            Retweet_created_at = DateTimeOffset.Now
        };

        // Inserta el retweet

        var insertResult = await _supabaseClient
            .From<RetweetsModel>()
            .Insert(retweetModel);

        Console.WriteLine("Inserta el retweet.");

        // Verifica si la inserción fue exitosa

        if (insertResult.Models == null || insertResult.Models.Count == 0)
        {
            Console.WriteLine("Error al insertar retweet.");
            return StatusCode(500, "Error al insertar el retweet.");
        }

        // Después de la inserción, realizar la búsqueda

        await Task.Delay(500);

        var _retweet = await _supabaseClient
            .From<RetweetsModel>()
            .Select("*")
            .Where(u => u.Retweet_original_id == tweet.Tweet_id && u.Retweet_user == parsedUserId)
            .Order("retweet_created_at", Supabase.Postgrest.Constants.Ordering.Descending)
            .Get();

        var retweet = _retweet.Models.FirstOrDefault();

        Console.WriteLine("Búsqueda.");

        if (retweet == null)
        {
            Console.WriteLine("Error al crear retweet.");
            return StatusCode(500, "Error al crear el retweet.");
        }

        // Creación del modelo de Tweet relacionado
        TweetsModel tweetModel = new()
        {
            Tweet_content = request.Content,
            Tweet_user_id = parsedUserId,
            Tweet_re_id = retweet.Retweet_id,
            Tweet_created_at = DateTimeOffset.Now // Se usa el ID del retweet que acabas de insertar
        };

        Console.WriteLine("Crea los modelos de tweet.");

        // Inserta el nuevo Tweet relacionado con el Retweet
        var tweetInsertResult = await _supabaseClient
            .From<TweetsModel>()
            .Insert(tweetModel);

        // Verifica si la inserción del tweet fue exitosa
        if (tweetInsertResult.Models == null || !tweetInsertResult.Models.Any())
        {
            return StatusCode(500, "Error al crear el tweet.");
        }

        return Ok("Retweet exitoso.");
    }

    // GET: Home/SearchUsernames
    [HttpGet]
    public async Task<IActionResult> SearchUsernames([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Query is required.");
        }

        try
        {
            var _response = await _supabaseClient
            .From<UsersModel>()
            .Select("*")
            .Filter("user_username", Supabase.Postgrest.Constants.Operator.ILike, $"%{query}%")
            .Get();

            if (_response.Models == null)
            {
                Console.WriteLine("Usuarios no encontrados.");
                return StatusCode((int)System.Net.HttpStatusCode.NoContent);
            }

            var response = _response.Models;

            if (response == null)
            {
                Console.WriteLine("Usuarios no encontrados.");
                return StatusCode((int)System.Net.HttpStatusCode.NoContent);
            }

            var _users = response.Select(user => new UserSearch
                        {
                            UserId = user.User_id.ToString(),
                            Username = user.User_username,
                            ProfilePicture = Url.Content(user.User_profile_picture_path)
                        }).ToList();

            return Ok(_users);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, "Internal server error occurred.");
        }

    }

    public class RetweetRequest
    {
        public Guid TweetId { get; set; }
        public string? Content { get; set; }
    }

    public class LikeRequest
    {
        public Guid TweetId { get; set; }
    }

    public class UserSearch
    {
        public required string UserId { get; set; }
        public required string Username { get; set; }
        public required string ProfilePicture { get; set; }
    }
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

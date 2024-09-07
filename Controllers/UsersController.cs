using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using twitter_clone.Models;
using twitter_clone.Services;
using System.Net;
using System.Net.Mail;

public class UsersController : Controller
{
    private readonly Supabase.Client _supabaseClient;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _hostingEnvironment;

    public UsersController(Supabase.Client supabaseClient, IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
    {
        _supabaseClient = supabaseClient;
        _configuration = configuration;
        _hostingEnvironment = hostingEnvironment;
    }

    // GET: Users/
    // Inicializador de variables globales y perfil (debe ser la primera página en cargar)
    public async Task<IActionResult> Index(bool buscado, string _username_buscado)
    {
        // Inicializar la variable global ViewBag con toda la información del usuario

        // Últimos usuarios

        var latestUsersResponse = await _supabaseClient
            .From<UsersModel>()
            .Select("*")
            .Order(u => u.User_created_at, Supabase.Postgrest.Constants.Ordering.Descending)
            .Limit(4)
            .Get();

        var latestUsers = latestUsersResponse.Models;

        ViewBag.LatestUsers = latestUsers;

        string? username;
        bool flag = false;

        if (!buscado)
        {
            username = Request.Cookies["UserName"];
        }
        else
        {
            username = _username_buscado;
            flag = true;
        }

        if (username == null)
        {
            return RedirectToAction("Login");
        }

        ViewBag.Username = username;

        var response = await _supabaseClient
                            .From<UsersModel>()
                            .Where(u => u.User_username == username)
                            .Get();

        if (response == null)
        {
            return RedirectToAction("Login");
        }

        var _user = response.Models.FirstOrDefault();

        if (_user == null)
        {
            return RedirectToAction("Login");
        }

        ViewBag.id = _user.User_id.ToString();

        if (flag)
        {
            var _userId = Guid.Parse(Request.Cookies["UserId"]);

            var _followed = await _supabaseClient
                                    .From<FollowsModel>()
                                    .Where(u => u.Follow_followed_id == _user.User_id && u.Follow_follower_id == _userId)
                                    .Get();

            var followed = _followed.Model;

            if (followed == null)
            {
                ViewBag.Followed = 0;
            }
            else
            {
                ViewBag.Followed = 1;
            }

            Console.WriteLine(ViewBag.Followed);
        }

        ViewBag.UserFirstName = _user.User_first_name;

        ViewBag.FormattedDate = _user.User_created_at.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);

        ViewBag.PicturePath = _user.User_profile_picture_path;

        ViewBag.Followers = _user.User_followers;

        ViewBag.Followings = _user.User_followings;

        ViewBag.Description = _user.User_description;

        ViewBag.BannerPicture = _user.User_banner_picture_path;

        var result = await _supabaseClient.From<TweetsModel>()
            .Select("*")
            .Where(x => x.Tweet_user_id == _user.User_id)
            .Order("tweet_created_at", Supabase.Postgrest.Constants.Ordering.Descending)
            .Get();

        if (result == null)
        {
            return View();
        }

        var tweets = result.Models;

        if (tweets == null)
        {
            return View();
        }

        // Inicializar la variable global de tweets y retweets del usuario

        ViewBag.TweetsOriginales = new List<TweetsModel>();

        ViewBag.Retweets = new List<TweetsModel>();

        ViewBag.RetweetsOriginales = new List<TweetsModel>();

        ViewBag.RetweetsOriginalesUsers = new List<UsersModel>();

        ViewBag._like = new List<int>();

        ViewBag.Cont = 0;

        ViewBag.LikesCont = 0;

        foreach (var tweet in tweets)
        {
            if (tweet.Tweet_re_id == null)
            {
                // Tweet Original, añadimos al ViewBag

                ViewBag.TweetsOriginales.Add(tweet);
            }
            else
            {
                // Rewtweet, recabamos más información

                ViewBag.Retweets.Add(tweet);

                var _re_result = await _supabaseClient.From<RetweetsModel>()
                    .Select(x => new object[] { x.Retweet_original_id })
                    .Where(x => x.Retweet_id == tweet.Tweet_re_id)
                    .Single();

                if (_re_result == null)
                {
                    ViewBag.Retweets.Clear();
                    return View();
                }

                var _ori_result = await _supabaseClient.From<TweetsModel>()
                    .Select(x => new object[] { x.Tweet_content, x.Tweet_created_at, x.Tweet_user_id, x.Tweet_picture_path })
                    .Where(x => x.Tweet_id == _re_result.Retweet_original_id)
                    .Single();

                if (_ori_result == null)
                {
                    ViewBag.Retweets.Clear();
                    return View();
                }

                ViewBag.RetweetsOriginales.Add(_ori_result);

                // Obtener al user del tweet original para desplegar su información

                var _ori_user = await _supabaseClient.From<UsersModel>()
                    .Select(x => new object[] { x.User_first_name, x.User_username, x.User_profile_picture_path })
                    .Where(x => x.User_id == _ori_result.Tweet_user_id)
                    .Single();

                if (_ori_user == null)
                {
                    ViewBag.Retweets.Clear();
                    return View();

                }

                ViewBag.RetweetsOriginalesUsers.Add(_ori_user);
            }

            var _like = await _supabaseClient.From<LikesModel>().Where(x => x.Like_tweet_id == tweet.Tweet_id).Single();

            if (_like == null)
            {
                // No se encontró un like
                ViewBag._like.Add(0);
            }
            else
            {
                ViewBag._like.Add(1);
            }
        }

        // Si todo es correcto, cargamos la página del perfil del usuario

        return View();
    }

    // GET: Users/Create
    public IActionResult Create()
    {
        if (Request.Cookies["UserName"] != null)
        {
            return RedirectToAction(nameof(Index));
        }

        return View();
    }

    // POST: Users/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UsersCreateModel user)
    {
        if (ModelState.IsValid)
        {
            // Validación personalizada para el username
            if (!user.User_username.StartsWith("@"))
            {
                ViewBag.Error = "Username must start with '@'.";
                ModelState.AddModelError("User_username", "Username must start with '@'.");
                return View(user);
            }

            UsersModel model = new UsersModel
            {
                User_first_name = user.User_first_name,
                User_last_name = user.User_last_name,
                User_username = user.User_username,
                User_email = user.User_email,
                User_password_hash = BCrypt.Net.BCrypt.EnhancedHashPassword(user.User_password_hash, 11),
                User_profile_picture_path = "~/assets/stock-profile-picture.webp",
                User_banner_picture_path = "~/assets/DefaultBanner.png"
            };

            try
            {
                var response = await _supabaseClient.From<UsersModel>().Insert(model);

                if (response.Models != null && response.Models.Count > 0)
                {
                    return RedirectToAction("Login");
                }
                else
                {
                    ViewBag.Error = "Error creating user.";
                    ModelState.AddModelError(string.Empty, "Error creating user.");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error creating user.";
                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
            }
        }
        else
        {
            ViewBag.Error = "Please submit the full form.";
            return View(user);
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Users/Login
    public IActionResult Login()
    {
        if (Request.Cookies["UserName"] == null)
        {
            return View();
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Users/Login
    [HttpPost]
    public async Task<IActionResult> Login(UsersLoginModel user)
    {
        var response = await _supabaseClient
                .From<UsersModel>()
                .Where(u => u.User_email == user.User_username_or_email || u.User_username == user.User_username_or_email)
                .Get();

        if (response != null)
        {
            var _user = response.Models.FirstOrDefault();
            if (_user != null)
            {
                ViewBag.SelfPicture = _user.User_profile_picture_path;
                if (BCrypt.Net.BCrypt.EnhancedVerify(user.User_password, _user.User_password_hash))
                {
                    var jwt = _configuration.GetSection("JwtSettings").Get<Jwt>();

                    if (jwt == null)
                    {
                        ViewBag.Error = "Internal error.";
                        return View(user);
                    }
                    var jwtToken = Authentication.GenerateJWTAuthentication(_user.User_id);

                    if (jwtToken == null)
                    {
                        ViewBag.Error = "Unauthorized login attempt.";
                        return View(user);
                    }
                    Console.WriteLine("Genera el token");

                    var userId = Authentication.ValidateToken(jwtToken);

                    if (string.IsNullOrEmpty(userId.ToString()))
                    {
                        ViewBag.Error = "Unauthorized login attempt.";
                        return View(user);
                    }

                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        // Secure = true, // Descomenta esta línea si tu aplicación está usando HTTPS
                        Expires = DateTime.Now.AddHours(1) // Configura la expiración de la cookie
                    };

                    // Añade la cookie
                    Response.Cookies.Append("jwt", jwtToken, cookieOptions);
                    Response.Cookies.Append("UserName", _user.User_username, new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(7) // Configura la expiración de la cookie para el UserName
                    });
                    Response.Cookies.Append("UserId", _user.User_id.ToString(), new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(7) // Configura la expiración de la cookie para el UserName
                    });
                    Response.Cookies.Append("SelfPicture", _user.User_profile_picture_path, new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(7) // Configura la expiración de la cookie para el UserName
                    });
                    Response.Cookies.Append("FirstName", _user.User_first_name, new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(7) // Configura la expiración de la cookie para el UserName
                    });
                    // Asigna valores a la sesión
                    HttpContext.Session.SetString("UserID", _user.User_id.ToString());
                    HttpContext.Session.SetString("UserName", _user.User_username.ToString());
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ViewBag.Error = "Unauthorized login attempt.";
                }
            }
            else
            {
                Console.WriteLine("No matching user.");
                ViewBag.Error = "No matching user. Try Signing in.";
            }
        }
        else
        {
            Console.WriteLine("No matching user.");
            ViewBag.Error = "No matching user. Try Signing in.";
        }

        return View(user);
    }

    // POST: Users/ModifyDescription
    [HttpPost]
    public async Task<IActionResult> ModifyDescription(string description)
    {
        if (!string.IsNullOrEmpty(description))
        {
            var username = Request.Cookies["UserName"];
            var response = await _supabaseClient
                .From<UsersModel>()
                .Where(u => u.User_username == username)
                .Get();

            var _user = response.Models.FirstOrDefault();

            if (_user == null)
            {
                return RedirectToAction(nameof(Index));
            }
            _user.User_description = description;

            await _supabaseClient.From<UsersModel>().Upsert(_user);


            TempData["SuccessMessage"] = "Descripción actualizada correctamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "La descripción no puede estar vacía.";
        }

        return RedirectToAction("Index");
    }

    // POST: Users/DeletePicture
    [HttpPost]
    public async Task<IActionResult> DeletePicture(int index)
    {
        var username = Request.Cookies["UserName"];

        var response = await _supabaseClient
                .From<UsersModel>()
                .Where(u => u.User_username == username)
                .Get();

        var _user = response.Models.FirstOrDefault();

        if (_user == null)
        {
            return RedirectToAction(nameof(Index));
        }

        if (index == 1)
        {
            Console.WriteLine("Elimina");
            _user.User_profile_picture_path = "~/assets/stock-profile-picture.webp";
            ViewBag.PicturePath = "~/assets/stock-profile-picture.webp";
        }
        else
        {
            _user.User_banner_picture_path = "~/assets/DefaultBanner.png";
        }

        await _supabaseClient.From<UsersModel>().Upsert(_user);

        return RedirectToAction(nameof(Index));
    }

    // POST: Users/UploadPicture
    [HttpPost]
    public async Task<IActionResult> UploadPicture(IFormFile profilePicture, string i)
    {
        string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "assets");
        string uniqueFileName = Guid.NewGuid().ToString() + "_" + profilePicture.FileName;
        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await profilePicture.CopyToAsync(fileStream);
        }

        var username = Request.Cookies["UserName"];

        var response = await _supabaseClient
                .From<UsersModel>()
                .Where(u => u.User_username == username)
                .Get();
        if (response == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var _user = response.Models.FirstOrDefault();

        if (_user == null)
        {
            return RedirectToAction(nameof(Index));
        }

        if (i == 1.ToString())
        {
            _user.User_profile_picture_path = "~/assets/" + uniqueFileName;
        }
        else
        {
            _user.User_banner_picture_path = "~/assets/" + uniqueFileName;
        }

        await _supabaseClient.From<UsersModel>().Upsert(_user);
        return RedirectToAction(nameof(Index));
    }

    // GET: Users/TestConnection
    // To test de USERS_TABLE database connection
    [HttpGet]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            var response = await _supabaseClient.From<UsersModel>().Get();

            if (response.Models != null && response.Models.Count > 0)
            {
                Console.WriteLine("Conexión exitosa. Usuarios encontrados:");
                foreach (var user in response.Models)
                {
                    Console.WriteLine($"Usuario: {user.User_first_name}, Email: {user.User_email}");
                }
                return Content("Conexión exitosa y datos obtenidos correctamente.");
            }
            else
            {
                return Content("Conexión exitosa, pero no se encontraron usuarios.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al conectar a la base de datos: {ex.Message}");
            return Content($"Error al conectar a la base de datos: {ex.Message}");
        }
    }

    // POST: Home/Like
    [HttpPost]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> Follow([FromBody] FollowRequest request)
    {
        string? input = Request.Cookies["UserId"];

        if (input == null)
        {
            return StatusCode(500, "Error al insertar el follow.");
        }

        string? destinatario, name, usuario;

        var original = Guid.Parse(input);
        var noOriginal = Guid.Parse(request.Followed);

        var _follow = await _supabaseClient
                            .From<FollowsModel>()
                            .Select("*")
                            .Where(u => u.Follow_followed_id == noOriginal && u.Follow_follower_id == original)
                            .Get();

        var Follow = _follow.Models.FirstOrDefault();

        var _userFollowed = await _supabaseClient
                            .From<UsersModel>()
                            .Select("*")
                            .Where(u => u.User_id == noOriginal)
                            .Get();

        var userFollowed = _userFollowed.Models.FirstOrDefault();


        if (userFollowed == null)
        {
            return StatusCode(500, "Error al insertar el follow.");
        }

        destinatario = userFollowed.User_email;
        Console.WriteLine(destinatario);
        name = userFollowed.User_first_name;
        usuario = userFollowed.User_username;

        var _userFollower = await _supabaseClient
                        .From<UsersModel>()
                        .Select("*")
                        .Where(u => u.User_id == original)
                        .Get();

        var userFollower = _userFollower.Models.FirstOrDefault();

        if (userFollower == null)
        {
            return StatusCode(500, "Error al insertar el follow.");
        }

        if (Follow == null)
        {
            // No existe el seguimiento
            FollowsModel newFollow = new()
            {
                Follow_followed_id = noOriginal,
                Follow_follower_id = original,
                Follow_created_at = DateTimeOffset.Now
            };

            var insertResult = await _supabaseClient
                                .From<FollowsModel>()
                                .Insert(newFollow);

            if (insertResult.Models == null || insertResult.Models.Count == 0)
            {
                return StatusCode(500, "Error al insertar el follow.");
            }

            // Actualizo al usuario

            userFollowed.User_followers++;

            userFollower.User_followings++;

            // Devolvemos los cambios

            var result = await _supabaseClient.From<UsersModel>()
                                              .Upsert(userFollowed);

            var _result = await _supabaseClient.From<UsersModel>()
                                              .Upsert(userFollower);
        }
        else
        {
            // Existe el seguimiento

            await _supabaseClient
                .From<FollowsModel>()
                .Where(u => u.Follow_followed_id == noOriginal && u.Follow_follower_id == original)
                .Delete();

            // Actualizo a los usuarios

            userFollowed.User_followers--;

            userFollower.User_followings--;

            // Devolvemos los cambios

            var result = await _supabaseClient.From<UsersModel>()
                                              .Upsert(userFollowed);

            var _result = await _supabaseClient.From<UsersModel>()
                                              .Upsert(userFollower);
        }

        // Validar si el email del destinatario es válido
        if (!IsValidEmail(destinatario))
        {
            throw new FormatException("El destinatario no tiene un formato de correo electrónico válido.");
        }

        const string subject = "Se ha actualizado tu lista de seguidores.";
        string body = "";

        if(Follow == null)
        {
            body = userFollower.User_first_name+" te ha empezado a seguir.";
        }
        else
        {
            body = userFollower.User_first_name+" ha dejado de seguirte";
        }

        NotificationsModel model = new()
        {
            Notification_user_id = userFollowed.User_id,
            Notification_content = body,
            Notification_created_at = DateTimeOffset.Now
        };

        var resp = await _supabaseClient.From<NotificationsModel>()
                                        .Insert(model);

        send(destinatario, name, body, subject);

        return Ok(Follow == null ? "Followed" : "Unfollowed");
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    [HttpGet]
    public IActionResult Logout()
    {
        if (Request.Cookies["UserId"] != null)
        {
            Response.Cookies.Delete("UserId");
        }

        // Opcionalmente, si estás usando otras cookies
        if (Request.Cookies["UserName"] != null)
        {
            Response.Cookies.Delete("UserName");
        }

        if (Request.Cookies["SelfPicture"] != null)
        {
            Response.Cookies.Delete("SelfPicture");
        }

        if (Request.Cookies["FirstName"] != null)
        {
            Response.Cookies.Delete("FirstName");
        }

        // Eliminar la sesión

        HttpContext.Session.Clear();

        return RedirectToAction("Login");
    }
    public class FollowRequest
    {
        public required string Followed { get; set; }
    }

    public async Task<IActionResult> ChatHub()
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

        // VIEWBAGS

        ViewBag.UsersList = new List<UsersModel>();

        var userId = Guid.Parse(Request.Cookies["UserId"]);
        ViewBag.userId = userId.ToString();
        var response = await _supabaseClient
                            .From<UsersModel>()
                            .Where(u => u.User_id == userId)
                            .Get();

        if (response == null)
        {
            return RedirectToAction("Login");
        }

        var _user = response.Models.FirstOrDefault();

        if (_user == null)
        {
            return RedirectToAction("Login");
        }

        var follows = await _supabaseClient
                            .From<FollowsModel>()
                            .Where(u => u.Follow_followed_id == userId)
                            .Get();
        if (follows == null)
        {
            return View();
        }

        var _follows = follows.Models;

        if (_follows == null)
        {
            return View();
        }

        foreach (var item in _follows)
        {
            var chat = await _supabaseClient
                            .From<UsersModel>()
                            .Where(u => u.User_id == item.Follow_follower_id)
                            .Get();

            if (chat == null)
            {
                return View();
            }

            var _chat = chat.Models.FirstOrDefault();

            if (_chat == null)
            {
                return View();
            }

            ViewBag.UsersList.Add(_chat);
        }

        return View();
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

    public class LoadMessagesRequest
    {
        public string Me { get; set; }
        public string Sender { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> LoadMessages([FromBody] LoadMessagesRequest request)
    {
        Console.WriteLine(request.Me);
        Console.WriteLine(request.Sender);
        if (request == null || string.IsNullOrEmpty(request.Me) || string.IsNullOrEmpty(request.Sender))
        {
            return StatusCode(500, "Inputs vacíos.");
        }
        var _me = Guid.Parse(request.Me);
        var _sender = Guid.Parse(request.Sender);

        // Mensajes entre los dos usuarios
        // Mensajes enviados por el usuario _me al usuario _sender
        var sentMessages = await _supabaseClient
            .From<MessagesModel>()
            .Where(u => u.Message_sender_id == _me && u.Message_receiver_id == _sender)
            .Get();

        // Mensajes enviados por el usuario _sender al usuario _me
        var receivedMessages = await _supabaseClient
            .From<MessagesModel>()
            .Where(u => u.Message_sender_id == _sender && u.Message_receiver_id == _me)
            .Get();

        // Combinar ambos resultados
        var chatMessages = sentMessages.Models.Concat(receivedMessages.Models)
            .OrderBy(m => m.Message_created_at)
            .ToList();

        var messages = chatMessages.Select(item => new
        {
            Content = item.Message_content,
            SentAt = item.Message_created_at
        }).ToList();

        // Devolver los mensajes en formato JSON
        return Json(messages);
    }

    public async Task<IActionResult> Mentions()
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

        string? input = Request.Cookies["UserId"];

        if (input == null)
        {
            return RedirectToAction("Login");
        }

        var _userId = Guid.Parse(input);

        var user = await _supabaseClient.From<NotificationsModel>()
                                        .Select("*")
                                        .Where( u => u.Notification_user_id == _userId)
                                        .Get();

        var _user = user.Models;
        ViewBag.Notifications = new List<NotificationsModel>();

        foreach (var item in _user)
        {
            ViewBag.Notifications.Add(item);
            Console.WriteLine(item.Notification_content);
        }

        return View();
    }

    public async Task<IActionResult> DeleteTweet(Guid id)
    {
        if (id ==Guid.Empty)
        {
            return RedirectToAction(nameof(Index));
        }

        await _supabaseClient.From<TweetsModel>()
                            .Where( t => t.Tweet_id == id)
                            .Delete();

        return RedirectToAction(nameof(Index));
    }


}

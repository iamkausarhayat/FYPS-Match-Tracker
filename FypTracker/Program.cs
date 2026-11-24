using FypTracker.Data;   //(using) Doosre folders aur packages ki classes istemal karne ki ijazat
using FypTracker.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args); //App banane ka "builder". Settings (appsettings, secrets) yahin load hoti hain

// Add services to the container.
builder.Services.AddControllersWithViews();     //MVC ka saman: controllers, views aur routing ke tools
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))); //Database ka darwaza banao, address DefaultConnection se lo
builder.Services.AddScoped<IProjectService, DbProjectService>();   //"Jab koi IProjectService maange, DbProjectService de do"
builder.Services.AddSingleton<IEmbedder, LocalEmbedderService>();  //AI model wala helper, poori app mein ek hi
builder.Services.AddSingleton<MeaningScorer>();    //Meaning score nikalne wala, ek hi
builder.Services.AddSingleton<OcrService>();       //Scanned PDF parhne wala (OCR), ek hi
builder.Services.AddScoped<SimilarityEngine>(); //Poora compare karne wala, har request ke liye naya
builder.Services.AddScoped<BrowseService>();    //Browse page aur home page ke top FYPs, har request ke liye naya

// Crossref (research papers): ek tayyar HttpClient jis mein address, timeout aur naam pehle se set hain
builder.Services.AddHttpClient<PaperSearchService>(client =>
{
    client.BaseAddress = new Uri("https://api.crossref.org/");
    client.Timeout = TimeSpan.FromSeconds(8);   // 8 second se zyada intezar nahi
    client.DefaultRequestHeaders.UserAgent.ParseAdd("FypTracker/1.0 (student project)");
});

// Live search: universities (DSpace) ki public search API. Settings "LiveSearch" section se, na ho to defaults.
builder.Services.Configure<LiveSearchOptions>(builder.Configuration.GetSection("LiveSearch"));
builder.Services.AddMemoryCache();                  // ek hi search ko kuch ghante yaad rakhne ke liye
builder.Services.AddHttpClient<DSpaceClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);       // ek request par 8 second se zyada intezar nahi
    client.DefaultRequestHeaders.UserAgent.ParseAdd("FypTracker/1.0 (student project)");   // sites ko pata ho ke kaun hai
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddScoped<LiveSearchService>();    // har request ke liye naya

// Suggestion box: template (offline) + optional AI. Timeout 10 second, uske baad template.
builder.Services.AddHttpClient<ISuggestionService, SuggestionService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Login: ek cookie jis mein user ka naam aur role hota hai
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/AdminLogin";          // bina login wale yahan bheje jate hain
        options.AccessDeniedPath = "/Account/AccessDenied"; // galat role wale yahan bheje jate hain
        options.Cookie.Name = "FypTracker.Auth";
        options.Cookie.HttpOnly = true;                     // JavaScript cookie nahi padh sakti
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

var app = builder.Build(); //Ab tak saman register kiya, ab app asal mein bani

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}"); //404/403 jaisay codes ko hamare khoobsurat /Home/Status page par bhej do

app.UseHttpsRedirection(); //http:// ko https:// par bhej do
app.UseRouting();  //URL dekh kar tay karo kaun sa controller

app.UseAuthentication();   // pehle: "aap kaun hain?" cookie deko 
app.UseAuthorization();    // phir: "kya aap ko ijazat hai?" role dekoo

app.MapStaticAssets();  //CSS, JS, tasveerein wwwroot se bhejo

app.MapControllerRoute( //URL ka dhancha (neeche misaal)
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run(); //Ab intezar shuru: request aane do
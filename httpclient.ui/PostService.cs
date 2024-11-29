namespace httpclient.ui;

public class Post
{
    public int userId { get; set; }
    public int id { get; set; }
    public string title { get; set; }
    public string body { get; set; }
}


public class PostService(HttpClient client)
{
    public async Task<Post?> GetByUserIdAsync(int userId)
    {
        var url = $"posts/{userId}";

        return await client.GetFromJsonAsync<Post>(url);
    }
}
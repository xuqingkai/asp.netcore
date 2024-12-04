using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

//加载配置文件
ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
//添加配置文件路径
configurationBuilder.SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
var configuration = configurationBuilder.Build();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.WebHost.UseUrls("http://0.0.0.0:9000");  //修改默认端口

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", (HttpContext context) => {
    context.Response.ContentType="text/html";
    string result = "";
    result += "[DateTime]=" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "<br />\r\n";
    result += "[Database]=" + configuration["Database:HostName"] + "<br />\r\n";
    result += "<hr />";
    result += "[OSVersion]=" + System.Environment.OSVersion + "<br />\r\n";
    result += "[.NETCore Version]=" + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription + "<br />\r\n";
    result += "[Version]=" + System.Environment.Version.Major + "." + System.Environment.Version.Minor + "." + System.Environment.Version.Build + "." + System.Environment.Version.Revision + "<br />\r\n";
    result += "[RemoteIP]=" + (context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? "" + context.Connection.RemoteIpAddress?.ToString()) + "<br />\r\n";
    result += "[IsHttps]=" + context.Request.IsHttps + "<br />\r\n";
    result += "<hr />";
    double timestamp = System.Math.Floor((System.DateTime.Now - System.Convert.ToDateTime("1970-01-01 00:00:00")).TotalSeconds);
    foreach(var key in context.Request.Headers.Keys){
        result += "[" + key + "]=" + context.Request.Headers[key] + "<br />\r\n";
    }
    return result;
});//



app.Map("/api/db/{table?}/{action?}/{id?}", (HttpContext context) =>{
    context.Request.ContentType = "application/x-www-form-urlencoded";
    context.Response.ContentType = "application/json";
    string ip = "" + context.Connection.RemoteIpAddress?.ToString();
    ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? ip;
    double timestamp = System.Math.Floor((System.DateTime.Now - System.Convert.ToDateTime("1970-01-01 00:00:00")).TotalSeconds);

    string table = context.Request.RouteValues["table"] + "";
    string action = context.Request.RouteValues["action"] + "";
    string id = context.Request.RouteValues["id"] + "";

    string databaseConnectionString = configuration["Database:SqlClient"] + "";
    System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(databaseConnectionString);
    if (connection.State != System.Data.ConnectionState.Open) { connection.Open(); }

    //int returnID = 0;
    //string? returnCode = null;
    string? returnError = null;
    string? returnData = null;
    if(table == "")
    {
        string? json = null;
        System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", connection);
        System.Data.SqlClient.SqlDataReader dataReader = command.ExecuteReader();
        while(dataReader.Read())
        {
            json += ",\"" + dataReader.GetString(0) + "\"";
        }
        if(!string.IsNullOrEmpty(json)){ json = json.Substring(1); }
        json =  "[" + json + "]";
        returnData = json;
    }
    else if(action == "save")
    {//new
        if(context.Request.Method != "POST")
        {
            returnError = "请求方法错误：" + context.Request.Method;
        }
        else{
            System.Collections.Specialized.NameValueCollection data = new System.Collections.Specialized.NameValueCollection(){
                {"ip", ip},
                //{"address", null},
                {"create_date", System.DateTime.Now.ToString("yyyy-MM-dd")},
                //{"create_datetime", null},
                //{"create_timestamp", null},
            };
            data.Set("create_datetime", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            string fields = "";
            string values = "";
            string updateSql = "";
            System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand();
            string?[] keys = data.AllKeys;
            if(keys != null)
            {
                foreach(string? key in keys)
                {
                    if(!string.IsNullOrEmpty(key))
                    {
                        if(!string.IsNullOrEmpty(data[key]) && context.Request.Form.Count>0 && context.Request.Form.ContainsKey(key)) { data.Set(key, context.Request.Form[key].FirstOrDefault()); }
                        fields += ",[" + key + "]";
                        values += ",@" + key + "";
                        updateSql += ",[" + key + "]=@" + key + "";
                        command.Parameters.Add(new System.Data.SqlClient.SqlParameter(key, data[key])); 
                    }
                } 
            }
            if(!string.IsNullOrEmpty(fields)){ fields = fields.Substring(1);}
            if(!string.IsNullOrEmpty(values)){ values = values.Substring(1);}
            if(!string.IsNullOrEmpty(updateSql)){ updateSql = updateSql.Substring(1);}

            string updateWhere = "";
            if(string.IsNullOrEmpty(id) || id=="0")
            {
                foreach(string key in context.Request.Query.Keys)
                {
                    updateWhere +=  " AND [" + key + "] = '" + context.Request.Query[key].FirstOrDefault() + "'";
                }
                if(!string.IsNullOrEmpty(updateWhere)){ updateWhere = updateWhere.Substring(5);}
            }
            else
            {
                updateWhere = " [id]=" + id;
            }

            command.Connection = connection;
            if(string.IsNullOrEmpty(updateWhere))
            {
                command.CommandText = "INSERT INTO [xqk_" + table + "] (" + fields + ") VALUES (" + values + ");";
            }
            else
            {
                command.CommandText = "UPDATE [xqk_" + table + "] SET " + updateSql + " WHERE " + updateWhere + ";";
            }
          
            //command.CommandType = System.Data.CommandType.StoredProcedure;
            int rows = command.ExecuteNonQuery();
            if(rows == 0)
            {
                returnError = "保存失败：" + command.CommandText;
            }
        }
    }
    else if(action == "list")
    {//index
        string sql = "SELECT * FROM [xqk_" + table + "] WHERE 1=1 ORDER BY ID DESC;";
        System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(sql, connection);
        System.Data.DataTable dataTable = new System.Data.DataTable();
        new System.Data.SqlClient.SqlDataAdapter(command).Fill(dataTable);
        string? json = null;
        foreach(System.Data.DataRow dataRow in dataTable.Rows){
            string item = "";
            foreach (System.Data.DataColumn dataColumn in dataRow.Table.Columns)
            {
            item += ",\"" + dataColumn.ColumnName + "\":\"" + dataRow[dataColumn].ToString() + "\""; 
            }
            if(!string.IsNullOrEmpty(item)){ item = item.Substring(1); }
            json += ",{" + item + "}";
        }
        if(!string.IsNullOrEmpty(json)){ json = json.Substring(1); }
        json =  "[" + json + "]";
        returnData = json;
    }
    else if(action == "show")
    {//detail
        string sql = "SELECT top 1 * FROM [xqk_" + table + "] WHERE [id]=" + id + " ORDER BY ID DESC;";
        System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(sql, connection);
        System.Data.DataTable dataTable = new System.Data.DataTable();
        new System.Data.SqlClient.SqlDataAdapter(command).Fill(dataTable);
        if (dataTable.Rows.Count > 0)
        {
            string? json = null;
            System.Data.DataRow dataRow = dataTable.Rows[0];
            foreach (System.Data.DataColumn dataColumn in dataRow.Table.Columns)
            {
            json += ",\"" + dataColumn.ColumnName + "\":\"" + dataRow[dataColumn].ToString() + "\""; 
            }
            if(!string.IsNullOrEmpty(json)){ json = "{" + json.Substring(1) + "}"; }
            returnData = json;
        }
    }
    else if(action == "delete")
    {
        string sql = "DELETE FROM [xqk_" + table + "] WHERE 1=1";
        System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand();
        command.Connection = connection;
        if(string.IsNullOrEmpty(id))
        {
            foreach(string key in context.Request.Query.Keys)
            {
            sql +=  " AND [" + key + "] = @" + key;
            command.Parameters.Add(new System.Data.SqlClient.SqlParameter(key, context.Request.Query[key].FirstOrDefault())); 
            }
        }
        else
        {
          sql +=  " AND [id] = @id";
          command.Parameters.Add(new System.Data.SqlClient.SqlParameter("id", id)); 
        }
        command.CommandText = sql;
        if(command.Parameters.Count == 0)
        {
            returnError = "请输入删除条件"; 
        }
        else
        {
            int rows = command.ExecuteNonQuery();
            if(rows == 0)
            {
                returnError = "删除失败:" + command.CommandText; 
            }
        }
    }
    else
    {
        returnError = action + "不存在";
    }
    
    connection.Close();
    connection.Dispose();
    string? returnJSON = null;
    if(returnError == null)
    {
        returnJSON = "\"id\":0,\"code\":\"SUCCESS\",\"message\":\"操作成功\"";
        if(returnData != null)
        {
            returnJSON += ",\"data\":" + returnData;
        }
        returnJSON = "{" + returnJSON + "}";
    }
    else
    {
        returnJSON = "{\"id\":1,\"code\":\"FAIL\",\"message\":\"" + returnError + "\"}";
    }
    return returnJSON;
});


app.MapGet("/ip/{action?}", (HttpContext context) => {
    string databaseConnectionString = configuration["Database:SqlClient"] + "";
    System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(databaseConnectionString);
    if (connection.State != System.Data.ConnectionState.Open) { connection.Open(); }
    string result = "";
    
    string action = context.Request.RouteValues["action"] + "";
    if(action == "save")
    {
        string ip = "" + context.Connection.RemoteIpAddress?.ToString();
        ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? ip;
        double timestamp = System.Math.Floor((System.DateTime.Now - System.Convert.ToDateTime("1970-01-01 00:00:00")).TotalSeconds);
        
        string sql = "SELECT * FROM [xqk_ip_log] WHERE [ip]='" + ip + "' AND [create_timestamp]>" + (timestamp - 60*20)  + " ORDER BY ID DESC;";
        System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(sql, connection);
        System.Data.DataTable dataTable = new System.Data.DataTable();
        new System.Data.SqlClient.SqlDataAdapter(command).Fill(dataTable);
        if (dataTable.Rows.Count == 0) {
            sql = "INSERT INTO [xqk_ip_log] ([ip], [address],[create_date], [create_datetime], [create_timestamp]) VALUES ('" + ip + "',N'测试','" + System.DateTime.Now.ToString("yyyy-MM-dd") + "', '" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', " + timestamp + ");";
            command = new System.Data.SqlClient.SqlCommand(sql, connection);
            int rows = command.ExecuteNonQuery();
        }
        result = ip; 
    }
    else
    {
        string sql = "SELECT * FROM [xqk_ip_log] WHERE 1=1 ORDER BY ID DESC;";
        System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(sql, connection);
        System.Data.DataTable dataTable = new System.Data.DataTable();
        new System.Data.SqlClient.SqlDataAdapter(command).Fill(dataTable);

        foreach(System.Data.DataRow dr in dataTable.Rows){
            result += dr["id"] + ",【" + dr["address"] + "】:" + dr["create_datetime"] + "\r\n";
        }
    }
    connection.Close();
	connection.Dispose();
    return result;
});

app.MapPost("/invoke", ([FromHeader(Name = "x-fc-request-id")] string requestId) =>{
    Console.WriteLine("get reqeuestId from header:" + requestId);
    return "get reqeuestId from header:" + requestId;
});

app.Run();

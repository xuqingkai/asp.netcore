using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.WebHost.UseUrls("http://0.0.0.0:9000");  //修改默认端口

//加载配置文件
ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
//添加配置文件路径
configurationBuilder.SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
var configuration = configurationBuilder.Build();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

// 注意：C# 为编译型语言，直接修改代码不能直接生效！请在控制台右上角“导出代码”，然后在编译代码后重新上传。
// 注意：C# 为编译型语言，直接修改代码不能直接生效！请在控制台右上角“导出代码”，然后在编译代码后重新上传。
// 注意：C# 为编译型语言，直接修改代码不能直接生效！请在控制台右上角“导出代码”，然后在编译代码后重新上传。
app.MapGet("/", (HttpContext context) => {

  string ip = "" + context.Connection.RemoteIpAddress?.ToString();
  ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? ip;
	double timestamp = System.Math.Floor((System.DateTime.Now - System.Convert.ToDateTime("1970-01-01 00:00:00")).TotalSeconds);
  string result = "【RemoteIpAddress】=" + ip + "\r\n";
  foreach(var key in context.Request.Headers.Keys){
      result += "【" + key + "】=" + context.Request.Headers[key] + "\r\n";
  }
  result += System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n";
  result += configuration["ConnectionString"];
  return result;
});//



app.Map("/api/db/{table}/{action}/{id?}", (HttpContext context) =>{
  context.Request.ContentType = "application/x-www-form-urlencoded";
  context.Response.ContentType = "application/json";
  string ip = "" + context.Connection.RemoteIpAddress?.ToString();
  ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? ip;
  double timestamp = System.Math.Floor((System.DateTime.Now - System.Convert.ToDateTime("1970-01-01 00:00:00")).TotalSeconds);

  string table = context.Request.RouteValues["table"] + "";
  string action = context.Request.RouteValues["action"] + "";
  string id = context.Request.RouteValues["id"] + "";

  string databaseConnectionString = "Persist Security Info=True;";
  databaseConnectionString += "Data Source=" + (context.Request.Query["ip"].FirstOrDefault() ?? "sql.bsite.net\\MSSQL2016") + ";";
  databaseConnectionString += "Initial Catalog=" + (context.Request.Query["ip"].FirstOrDefault() ?? "xuqingkai_com") + ";";
  databaseConnectionString += "User ID=" + (context.Request.Query["ip"].FirstOrDefault() ?? "xuqingkai_com") + ";";
  databaseConnectionString += "Password=" + (context.Request.Query["ip"].FirstOrDefault() ?? "1234qwer") + ";";
  System.Data.SqlClient.SqlConnection sqlConnection = new System.Data.SqlClient.SqlConnection(databaseConnectionString);
  if (sqlConnection.State != System.Data.ConnectionState.Open) { sqlConnection.Open(); }

  //int returnID = 0;
  //string? returnCode = null;
  string? returnError = null;
  string? returnData = null;

  if(action == "save"){//new
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
      System.Data.SqlClient.SqlCommand sqlCommand = new System.Data.SqlClient.SqlCommand();
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
                sqlCommand.Parameters.Add(new System.Data.SqlClient.SqlParameter(key, data[key])); 
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

      sqlCommand.Connection = sqlConnection;
      if(string.IsNullOrEmpty(updateWhere))
      {
        sqlCommand.CommandText = "INSERT INTO [xqk_" + table + "] (" + fields + ") VALUES (" + values + ");";
      }
      else
      {
        sqlCommand.CommandText = "UPDATE [xqk_" + table + "] SET " + updateSql + " WHERE " + updateWhere + ";";
      }
      
      //sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
      int rows = sqlCommand.ExecuteNonQuery();
      if(rows == 0)
      {
        returnError = "保存失败：" + sqlCommand.CommandText;
      }
    }
  }else if(action == "list"){//index
    string sql = "SELECT * FROM [xqk_" + table + "] WHERE 1=1 ORDER BY ID DESC;";
    System.Data.SqlClient.SqlCommand sqlCommand = new System.Data.SqlClient.SqlCommand(sql, sqlConnection);
    System.Data.DataTable dataTable = new System.Data.DataTable();
    new System.Data.SqlClient.SqlDataAdapter(sqlCommand).Fill(dataTable);
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
  }else if(action == "show"){//detail
    string sql = "SELECT top 1 * FROM [xqk_" + table + "] WHERE [id]=" + id + " ORDER BY ID DESC;";
    System.Data.SqlClient.SqlCommand sqlCommand = new System.Data.SqlClient.SqlCommand(sql, sqlConnection);
    System.Data.DataTable dataTable = new System.Data.DataTable();
    new System.Data.SqlClient.SqlDataAdapter(sqlCommand).Fill(dataTable);
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
  }else if(action == "delete"){
    string sql = "DELETE FROM [xqk_" + table + "] WHERE 1=1";
    System.Data.SqlClient.SqlCommand sqlCommand = new System.Data.SqlClient.SqlCommand();
    sqlCommand.Connection = sqlConnection;
    if(string.IsNullOrEmpty(id))
    {
      foreach(string key in context.Request.Query.Keys)
      {
        sql +=  " AND [" + key + "] = @" + key;
        sqlCommand.Parameters.Add(new System.Data.SqlClient.SqlParameter(key, context.Request.Query[key].FirstOrDefault())); 
      }
    }
    else
    {
      sql +=  " AND [id] = @id";
      sqlCommand.Parameters.Add(new System.Data.SqlClient.SqlParameter("id", id)); 
    }
    
    sqlCommand.CommandText = sql;
    if(sqlCommand.Parameters.Count == 0)
    {
      returnError = "请输入删除条件"; 
    }
    else
    {
      int rows = sqlCommand.ExecuteNonQuery();
      if(rows == 0)
      {
        returnError = "删除失败:" + sqlCommand.CommandText; 
      }
    }
  }else{
    returnError = action + "不存在";
  }
  sqlConnection.Close();
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


app.MapGet("/ip", (HttpContext context) => {
  string ip = "" + context.Connection.RemoteIpAddress?.ToString();
  ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? ip;
	double timestamp = System.Math.Floor((System.DateTime.Now - System.Convert.ToDateTime("1970-01-01 00:00:00")).TotalSeconds);

  string databaseConnectionString = "Persist Security Info=True;";
  databaseConnectionString += "Data Source=" + (context.Request.Query["ip"].FirstOrDefault() ?? "sql.bsite.net\\MSSQL2016") + ";";
  databaseConnectionString += "Initial Catalog=" + (context.Request.Query["ip"].FirstOrDefault() ?? "xqk_test") + ";";
  databaseConnectionString += "User ID=" + (context.Request.Query["ip"].FirstOrDefault() ?? "sa") + ";";
  databaseConnectionString += "Password=" + (context.Request.Query["ip"].FirstOrDefault() ?? "1234qwer") + ";";
  System.Data.SqlClient.SqlConnection sqlConnection = new System.Data.SqlClient.SqlConnection(databaseConnectionString);
  if (sqlConnection.State != System.Data.ConnectionState.Open) { sqlConnection.Open(); }

  string sql = "SELECT * FROM [xqk_ip_log] WHERE [ip]='" + ip + "' AND [create_timestamp]>" + (timestamp - 60*20)  + " ORDER BY ID DESC;";
	System.Data.SqlClient.SqlCommand sqlCommand = new System.Data.SqlClient.SqlCommand(sql, sqlConnection);
	System.Data.DataTable dataTable = new System.Data.DataTable();
	new System.Data.SqlClient.SqlDataAdapter(sqlCommand).Fill(dataTable);
	if (dataTable.Rows.Count == 0) {
		sql = "INSERT INTO [xqk_ip_log] ([ip], [create_date], [create_datetime], [create_timestamp]) VALUES ('" + ip + "', '" + System.DateTime.Now.ToString("yyyy-MM-dd") + "', '" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', " + timestamp + ");";
		sqlCommand = new System.Data.SqlClient.SqlCommand(sql, sqlConnection);
		int rows = sqlCommand.ExecuteNonQuery();
	}
  sql = "SELECT * FROM [xqk_ip_log] WHERE 1=1 ORDER BY ID DESC;";
	sqlCommand = new System.Data.SqlClient.SqlCommand(sql, sqlConnection);
	dataTable = new System.Data.DataTable();
	new System.Data.SqlClient.SqlDataAdapter(sqlCommand).Fill(dataTable);

  string result = "";
  foreach(System.Data.DataRow dr in dataTable.Rows){
      result += dr["id"] + ",【" + dr["ip"] + "】:" + dr["create_datetime"] + "\r\n";
  }
  return result;
});

app.MapPost("/invoke", ([FromHeader(Name = "x-fc-request-id")] string requestId) =>{
  // 注意：C# 为编译型语言，直接修改代码不能直接生效！请在控制台右上角“导出代码”，然后在编译代码后重新上传。
	// 注意：C# 为编译型语言，直接修改代码不能直接生效！请在控制台右上角“导出代码”，然后在编译代码后重新上传。
	// 注意：C# 为编译型语言，直接修改代码不能直接生效！请在控制台右上角“导出代码”，然后在编译代码后重新上传。
	// Notice: You need to complie the code first otherwise the code change will not
	// take effect.
  Console.WriteLine("get reqeuestId from header:" + requestId);
  return "get reqeuestId from header:" + requestId;
});

app.Run();

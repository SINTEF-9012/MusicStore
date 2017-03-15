using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using MusicStore.Models;
using System.Text.RegularExpressions;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using System.Reflection;

namespace MusicStore.Customiser
{

    public class CallBack
    {
        public JObject body;
        public string function;
    }

    public class Manual
    {
        public string returnx;
        public string comment;
        public CallBack callback;
        public JObject context;
        public List<string> instructions;
    }

    public class RestUtil
    {
        public static RestUtil instance = new RestUtil();
        private HttpClient client;

        protected RestUtil()
        {
            client = new HttpClient();
        }

        public async Task<Manual> Post(string url, JObject body)
        {
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            
            var result = client.PostAsync(url, content).Result;
            string strResult = await result.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine(strResult);
            if (strResult.StartsWith("Cannot"))
                return null;
            return JsonConvert.DeserializeObject<Manual>(strResult);
        }
    }

    public class Interpreter
    {
        public static Interpreter instance = new Interpreter();

        private Interpreter()
        {
            var type = typeof(DynamicExpressionParser).GetTypeInfo().Assembly.GetType("System.Linq.Dynamic.Core.ExpressionParser");
            FieldInfo field = type.GetField("_predefinedTypes", BindingFlags.Static | BindingFlags.NonPublic);

            Dictionary<Type, int> predefinedTypes = (Dictionary<Type, int>)field.GetValue(null);

            predefinedTypes[typeof(Microsoft.AspNetCore.Mvc.JsonResult)] = 0; // Your type
            predefinedTypes[typeof(Microsoft.AspNetCore.Mvc.ContentResult)] = 0;
            predefinedTypes[typeof(MusicStore.Models.CartItem)] = 0;
            predefinedTypes[typeof(Task<List<CartItem>>)] = 0;
            //predefinedTypes[typeof(System.Collections.Generic.List)] = 0;

            field.SetValue(null, predefinedTypes);
        }

        public object EvaluateCommand(string command, Dictionary<string, object> context)
        {
            if (command.StartsWith("SET"))
            {
                command = Regex.Replace(command, "SET", "");
                int eq = command.IndexOf('=');
                var leftvalue = command.Substring(0, eq).Trim();
                var rightvalue = command.Substring(eq + 1);

                var lastdot = leftvalue.LastIndexOf('.');
                var lefthead = leftvalue.Substring(0, lastdot);
                var leftprop = leftvalue.Substring(lastdot + 1);

                var host = EvaluateCommand(lefthead, context);
                var right = EvaluateCommand(rightvalue, context);
                host.GetType().GetProperty(leftprop).SetValue(host, right);
                return "void";

            }
            else
                return EvaluateExpr(command, context);
        }

        public object EvaluateExpr(string command, Dictionary<string, object> context)
        {
            Regex pattern = new Regex(@"\$\w+");
            var matches = pattern.Matches(command);
            var paramlist = new List<ParameterExpression>();
            var arguments = new List<object>();
            foreach(var variable in matches)
            {
                var k = variable.ToString().Substring(1);
                var v = context[k];
                var p = Expression.Parameter(v.GetType(), k);
                paramlist.Add(p);
                arguments.Add(v);
            }
            string cleancommand = Regex.Replace(command, @"\$", "");
            var e = DynamicExpressionParser.ParseLambda(true, paramlist.ToArray(), typeof(object), cleancommand);
            return e.Compile().DynamicInvoke(arguments.ToArray());
            
        }
        public static object Evaluate(string command, Dictionary<string, object> context)
        {
            /* Now works for:
             * - $album.ArtUrl
             * - String.format($str_form, $endpoint, $newitem.CartItemId.ToString())
             * - $this.Content($form)
             * - $this.GetCartItems().Result
             * - $cart.GetCartItems().Result.FirstOrDefault(AlbumId == $id)
             * - SET $content.ContentType = $str_contenttype
             * - SET $album.AlbumArtUrl = $str_url
             */
            return Interpreter.instance.EvaluateCommand(command, context);
        }

    }
}

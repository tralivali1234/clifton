﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceManagement;
using Clifton.WebInterfaces;

namespace Clifton.WebRouterService
{
	/// <summary>
	/// Maps a route to the semantic type that processes that route.
	/// The route key is the verb and path, the route dictionary value is the semantic type to instantiate
	/// for that route.  We assume here that the semantic type will be populated from deserialized JSON data
	/// that is in the request input stream.
	/// If the route key is not found in the web service route dictionary, then the UnhandledContext semantic
	/// type is instantiated on the WebServerMembrane bus.  This is (default) handled in the WebFileResponseService module,
	/// by the UnhandledContext receptor.
	/// </summary>
	public class AuthenticatingRouterReceptor : IReceptor
	{
		public void Process(ISemanticProcessor proc, IMembrane membrane, Route route)
		{
			IAuthenticatingRouterService routerService = proc.ServiceManager.Get<IAuthenticatingRouterService>();
			HttpListenerContext context = route.Context;
			string data = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding).ReadToEnd();
			HttpVerb verb = context.Verb();
			UriPath path = context.Path();
			string searchRoute = GetSearchRoute(verb, path);
			RouteInfo routeInfo;

			// TODO: Session manager may not exist.  How do we handle services that are missing?
			IWebSessionService session = proc.ServiceManager.Get<IWebSessionService>();

			// Semantic routes can be either public or authenticated.
			if (routerService.Routes.TryGetValue(searchRoute, out routeInfo))
			{
				// Public routes always authenticate.
				bool authenticatedRoute = true;
				bool authorizedRoute = true;

				if (routeInfo.RouteType == RouteType.AuthenticatedRoute)
				{
					session.UpdateState(context);
					authenticatedRoute = session.IsAuthenticated(context);
				}

				if (routeInfo.RouteType == RouteType.RoleRoute)
				{
					session.UpdateState(context);
					authenticatedRoute = session.IsAuthenticated(context);

					// User must be authenticated and have the correct role setting.
					if (authenticatedRoute)
					{
						// Any bits that are set with a binary "and" of the route's role mask and the current role passes the authorization test.	
						uint mask = session.GetSessionObject<uint>(context, "RoleMask");
						authorizedRoute = (mask & routeInfo.RoleMask) != 0;
					}
				}

				if (authenticatedRoute && authorizedRoute)
				{
					Type receptorSemanticType = routeInfo.ReceptorSemanticType;
					SemanticRoute semanticRoute = (SemanticRoute)Activator.CreateInstance(receptorSemanticType);
					semanticRoute.PostData = data;

					if (!String.IsNullOrEmpty(data))
					{
						// Is it JSON?
						if (data[0] == '{')
						{
							JsonConvert.PopulateObject(data, semanticRoute);
						}
						else
						{
							// Example: "username=sdfsf&password=sdfsdf&LoginButton=Login"
							string[] parms = data.Split('&');

							foreach (string parm in parms)
							{
								string[] keyVal = parm.Split('=');
								PropertyInfo pi = receptorSemanticType.GetProperty(keyVal[0], BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

								if (pi != null)
								{
									// TODO: Convert to property type.
									// TODO: value needs to be re-encoded to handle special characters.
									pi.SetValue(semanticRoute, keyVal[1]);
								}
							}
						}
					}
					else if (verb.Value == "GET")
					{
						// Parse parameters
						NameValueCollection nvc = context.Request.QueryString;

						foreach (string key in nvc.AllKeys)
						{
							PropertyInfo pi = receptorSemanticType.GetProperty(key, BindingFlags.Public | BindingFlags.Instance);

							if (pi != null)
							{
								// TODO: Convert to property type.
								// TODO: value needs to be re-encoded to handle special characters.
								pi.SetValue(semanticRoute, nvc[key]);
							}
						}
					}

					// Must be done AFTER populating the object -- sometimes the json converter nulls the base class!
					semanticRoute.Context = context;
					proc.ProcessInstance<WebServerMembrane>(semanticRoute, true);
				}
				else
				{
					// Deal with expired or requires authentication.
					switch (session.GetState(context))
					{
						case SessionState.New:
							proc.ProcessInstance<WebServerMembrane, StringResponse>(r =>
							{
								r.Context = context;
								r.Message = "authenticationRequired";		// used in clifton.spa.js to handle SPA error responses
								r.StatusCode = 403;
							});

							break;

						case SessionState.Authenticated:
							proc.ProcessInstance<WebServerMembrane, StringResponse>(r =>
							{
								r.Context = context;
								r.Message = "notAuthorized";				// used in clifton.spa.js to handle SPA error responses
								r.StatusCode = 401;
							});
							break;

						case SessionState.Expired:
							proc.ProcessInstance<WebServerMembrane, StringResponse>(r =>
							{
								r.Context = context;
								r.Message = "sessionExpired";				// used in clifton.spa.js to handle SPA error responses
								r.StatusCode = 401;
							});

							break;
					}
				}
			}
/* Nothing for the authenticated router to do.  Let the public router handle it. 
			else
			{
				// Put the context on the bus for some service to pick up.
				// All unhandled context are assumed to be public routes.
				proc.ProcessInstance<WebServerMembrane, UnhandledContext>(c => c.Context = context);
			}
 */ 
		}

		protected string GetSearchRoute(HttpVerb verb, UriPath path)
		{
			return verb.Value + ":" + path.Value;
		}
	}
}

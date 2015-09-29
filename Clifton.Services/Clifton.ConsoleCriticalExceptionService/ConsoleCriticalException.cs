﻿using System;

using Clifton.ServiceInterfaces;

namespace Clifton.ConsoleCriticalExceptionService
{
	public class ConsoleCriticalExceptionModule : IModule
	{
		public virtual void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IConsoleCriticalExceptionService, ConsoleCriticalException>();
		}
	}

    public class ConsoleCriticalException : ServiceBase, IConsoleCriticalExceptionService
    {
		public override void Initialize(IServiceManager svcMgr)
		{
			base.Initialize(svcMgr);
			AppDomain.CurrentDomain.UnhandledException += GlobalExceptionHandler;
		}

		void GlobalExceptionHandler(object sender, UnhandledExceptionEventArgs e)
		{
			try
			{
				ILoggerService logger = serviceManager.Get<ILoggerService>();

				if (e.ExceptionObject is Exception)
				{
					logger.Log((Exception)e.ExceptionObject);
				}
				else
				{
					logger.Log(ExceptionMessage.Create(e.ExceptionObject.GetType().Name));
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
			}

			Environment.Exit(1);
		}
    }
}
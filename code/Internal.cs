using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sandbox;
using Sandbox.DataModel;

namespace SymlinkWatcher;

public static class Internal
{
	private static readonly Type LocalProjectType;
	private static readonly Type ServerContentType;
	private static readonly Type BaseContentType;
	private static readonly Type RuntimeContentType;
	private static readonly Type ToolAddonManagerType;
	private static readonly Type ToolAddonType;
	private static readonly Type CompilerType;

	static Internal()
	{
		try
		{
			LocalProjectType = typeof(LocalProject);

			var engineAssembly = Assembly.Load( "Sandbox.Engine" );
			var toolAssembly = Assembly.Load( "Sandbox.Tools" );
			ServerContentType = engineAssembly.GetType( "Sandbox.ServerContent" );
			BaseContentType = engineAssembly.GetType( "Sandbox.BaseContent" );
			CompilerType = engineAssembly.GetType( "Sandbox.Compiler" );
			RuntimeContentType = engineAssembly.GetType( "Sandbox.RuntimeContent" );
			ToolAddonManagerType = toolAssembly.GetType( "Editor.ToolAddonManager" );
			ToolAddonType = toolAssembly.GetType( "Editor.ToolAddon" );
		}
		catch ( Exception e )
		{
			// without these static try blocks s&box seems to not load the tool??
		}
	}

	public static List<LocalProject> GetAllLocalProjects()
	{
		var field = LocalProjectType.GetField( "All", BindingFlags.Static | BindingFlags.NonPublic );
		if ( field == null )
			throw new NullReferenceException( "Field LocalProject.All == null" );
		return field.GetValue( null ) as List<LocalProject>;
	}

	public static IEnumerable<object> GetAllServerContents()
	{
		var property = ServerContentType.GetProperty( "All", BindingFlags.Static | BindingFlags.NonPublic );
		if ( property == null )
			throw new NullReferenceException( "Property ServerContent.All == null" );
		return (IEnumerable<object>)property.GetValue( null );
	}

	public static IEnumerable<object> GetAllToolAddons()
	{
		var field = ToolAddonManagerType.GetField( "All", BindingFlags.Static | BindingFlags.NonPublic );
		if ( field == null )
			throw new NullReferenceException( "Field ToolAddonManager.All == null" );
		return (IEnumerable<object>)field.GetValue( null );
	}

	private static string GetBaseContentIdent( object content )
	{
		var property = BaseContentType.GetProperty( "Ident" );
		if ( property == null )
			throw new NullReferenceException( "Property BaseContent.Ident == null" );
		return (string)property.GetValue( content );
	}

	private static string GetToolAddonIdent( object addon )
	{
		var property = ToolAddonType.GetProperty( "Config" );
		if ( property == null )
			throw new NullReferenceException( "Property ToolAddon.Config == null" );
		var config = (ProjectConfig)property.GetValue( addon );
		return config?.FullIdent;
	}

	public static object GetServerContentByIdent( string ident )
	{
		return GetAllServerContents().FirstOrDefault( content => string.Equals( ident.Replace( "#local", "" ),
			GetBaseContentIdent( content ), StringComparison.OrdinalIgnoreCase ) );
	}

	public static object GetToolAddonByIdent( string ident )
	{
		return GetAllToolAddons().FirstOrDefault( addon => string.Equals( ident.Replace( "#local", "" ),
			GetToolAddonIdent( addon ), StringComparison.OrdinalIgnoreCase ) );
	}

	public static object GetRuntimeContentCompiler( object content )
	{
		var field = RuntimeContentType.GetField( "compiler", BindingFlags.Instance | BindingFlags.NonPublic );
		if ( field == null )
			throw new NullReferenceException( "Field RuntimeContent.compiler == null" );
		return field.GetValue( content );
	}

	public static object GetToolAddonCompiler( object addon )
	{
		var property = ToolAddonType.GetProperty( "Compiler", BindingFlags.Instance | BindingFlags.NonPublic );
		if ( property == null )
			throw new NullReferenceException( "Property ToolAddon.Compiler == null" );
		return property.GetValue( addon );
	}

	public static void MarkForRecompile( object compiler )
	{
		var method = CompilerType.GetMethod( "MarkForRecompile", BindingFlags.Instance | BindingFlags.NonPublic );
		if ( method == null )
			throw new NullReferenceException( "Method Compiler.MarkForRecompile == null" );
		method.Invoke( compiler, null );
	}

	[Flags]
	public enum NotifyFilters
	{
		FileName = 1,
		DirectoryName = 2,
		Attributes = 4,
		Size = 8,
		LastWrite = 16, // 0x00000010
		LastAccess = 32, // 0x00000020
		CreationTime = 64, // 0x00000040
		Security = 256, // 0x00000100
	}
}

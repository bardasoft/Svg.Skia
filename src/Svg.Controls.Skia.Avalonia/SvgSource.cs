﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Avalonia.Metadata;
using SkiaSharp;
using Svg;
using Svg.Model;
using Svg.Model.Services;
using Svg.Skia;

namespace Avalonia.Svg.Skia;

/// <summary>
/// Represents a <see cref="SkiaSharp.SKPicture"/> based image.
/// </summary>
[TypeConverter(typeof(SvgSourceTypeConverter))]
public sealed class SvgSource : IDisposable
{
    public static readonly ISvgAssetLoader s_assetLoader;

    public static readonly SkiaModel s_skiaModel;

    private readonly Uri? _baseUri;
    private SKPicture? _picture;
    private SvgParameters? _originalParameters;
    private string? _originalPath;
    private Stream? _originalStream;

    [Content]
    public string? Path { get; init; }

    public Dictionary<string, string>? Entities { get; init; }

    public string? Css { get; init; }

    public SvgParameters? Parameters => _originalParameters;

    public SKPicture? Picture
    {
        get
        {
            if (_picture is null && Path is not null)
            {
                _picture = LoadImpl(this, Path, _baseUri, new SvgParameters(Entities, Css));
            }

            return _picture;
        }
        set => _picture = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SvgSource"/> class.
    /// </summary>
    /// <param name="baseUri">The base URL for the XAML context.</param>
    public SvgSource(Uri? baseUri)
    {
        _baseUri = baseUri;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SvgSource"/> class.
    /// </summary>
    /// <param name="serviceProvider">The XAML service provider.</param>
    public SvgSource(IServiceProvider serviceProvider)
    {
        _baseUri = serviceProvider.GetContextBaseUri();
    }

    public void Dispose()
    {
        lock (Sync)
        {
            _picture?.Dispose();

            _originalPath = null;
            _originalStream?.Dispose();
            _originalStream = null;
        }
    }

    /// <summary>
    /// Enable throw exception on missing resource.
    /// </summary>
    public static bool EnableThrowOnMissingResource { get; set; }

    public object Sync { get; } = new ();

    static SvgSource()
    {
        s_skiaModel = new SkiaModel(new SKSvgSettings());
        s_assetLoader = new SkiaSvgAssetLoader(s_skiaModel);
    }

    private static SKPicture? Load(SvgSource source, string? path, SvgParameters? parameters)
    {
        source._originalPath = path;
        source._originalStream?.Dispose();
        source._originalStream = null;

        if (path is null)
        {
            return null;
        }
        
        var svgDocument = SvgService.Open(path, parameters);
        if (svgDocument is null)
        {
            return null;
        }
            
        var model = SvgService.ToModel(svgDocument, s_assetLoader, out _, out _);

        return s_skiaModel.ToSKPicture(model);
    }

    private static SKPicture? Load(SvgSource source, Stream stream, SvgParameters? parameters = null)
    {
        if (source._originalStream != stream)
        {
            source._originalStream?.Dispose();
            source._originalStream = new MemoryStream();
            stream.CopyTo(source._originalStream);
        }

        source._originalPath = null;
        source._originalParameters = parameters;
        source._originalStream.Position = 0;

        var svgDocument = SvgService.Open(source._originalStream, parameters);
        if (svgDocument is null)
        {
            return null;
        }

        var model = SvgService.ToModel(svgDocument, s_assetLoader, out _, out _);

        return s_skiaModel.ToSKPicture(model);
    }

    private static SKPicture? FromSvg(string svg)
    {
        var svgDocument = SvgService.FromSvg(svg);
        if (svgDocument is { })
        {
            var model = SvgService.ToModel(svgDocument, s_assetLoader, out _, out _);

            return s_skiaModel.ToSKPicture(model);
        }
        return null;
    }

    private static SKPicture? FromSvgDocument(SvgDocument? svgDocument)
    {
        if (svgDocument is { })
        {
            var model = SvgService.ToModel(svgDocument, s_assetLoader, out _, out _);

            return s_skiaModel.ToSKPicture(model);
        }
        return null;
    }

    private static SvgSource? ThrowOnMissingResource(string path)
    {
        return EnableThrowOnMissingResource 
            ? throw new ArgumentException($"Invalid resource path: {path}") 
            : null;
    }

    private static SKPicture? LoadImpl(SvgSource source, string path, Uri? baseUri, SvgParameters? parameters = null)
    {
        if (File.Exists(path))
        {
            return Load(source, path, parameters);
        }

        if (Uri.TryCreate(path, UriKind.Absolute, out var uriHttp) && (uriHttp.Scheme == "http" || uriHttp.Scheme == "https"))
        {
            try
            {
                var response = new HttpClient().GetAsync(uriHttp).Result;
                if (response.IsSuccessStatusCode)
                {
                    var stream = response.Content.ReadAsStreamAsync().Result;
                    return Load(source, stream, parameters);
                }
            }
            catch (HttpRequestException e)
            {
                Debug.WriteLine("Failed to connect to " + uriHttp);
                Debug.WriteLine(e.ToString());
            }

            ThrowOnMissingResource(path);
            return null;
        }
        
        var uri = path.StartsWith("/") ? new Uri(path, UriKind.Relative) : new Uri(path, UriKind.RelativeOrAbsolute);
        if (uri.IsAbsoluteUri && uri.IsFile)
        {
            return Load(source, uri.LocalPath, parameters);
        }
        else
        {
            var stream = Platform.AssetLoader.Open(uri, baseUri);
            if (stream is null)
            {
                ThrowOnMissingResource(path);
                return null;
            }
            return Load(source, stream, parameters);
        }
    }

    /// <summary>t
    /// Loads svg source from file or resource.
    /// </summary>
    /// <param name="path">The path to file or resource.</param>
    /// <param name="baseUri">The base uri.</param>
    /// <param name="parameters">The svg parameters.</param>
    /// <returns>The svg source.</returns>
    public static SvgSource? Load(string path, Uri? baseUri = default, SvgParameters? parameters = null)
    {
        var source = new SvgSource(baseUri);
        source._picture = LoadImpl(source, path, baseUri, parameters);
        return source;
    }

    /// <summary>
    /// Loads svg source from svg source.
    /// </summary>
    /// <param name="svg">The svg source.</param>
    /// <returns>The svg source.</returns>
    public static SvgSource? LoadFromSvg(string svg)
    {
        var source = new SvgSource(default(Uri));
        source._picture = FromSvg(svg);
        return source;
    }

    /// <summary>t
    /// Loads svg source from stream.
    /// </summary>
    /// <param name="stream">The svg stream.</param>
    /// <param name="parameters">The svg parameters.</param>
    /// <returns>The svg source.</returns>
    public static SvgSource? LoadFromStream(Stream stream, SvgParameters? parameters = null)
    {
        var source = new SvgSource(default(Uri));
        source._picture = Load(source, stream, parameters);
        return source;
    }

    /// <summary>t
    /// Loads svg source from svg document.
    /// </summary>
    /// <param name="document">The svg document.</param>
    /// <returns>The svg source.</returns>
    public static SvgSource? LoadFromSvgDocument(SvgDocument document)
    {
        var source = new SvgSource(default(Uri));
        source._picture = FromSvgDocument(document);
        return source;
    }

    public void ReLoad(SvgParameters? parameters)
    {
        lock (Sync)
        {
            _picture = null;

            _originalParameters = parameters;

            if (_originalStream == null)
            {
                _picture = Load(this, _originalPath, parameters);
                return;
            }

            _originalStream.Position = 0;

            _picture = Load(this, _originalStream, parameters);
        }
    }
}

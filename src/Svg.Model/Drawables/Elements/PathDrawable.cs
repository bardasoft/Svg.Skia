﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using System.Collections.Generic;
using ShimSkiaSharp;
using Svg.Model.Services;

namespace Svg.Model.Drawables.Elements;

public sealed class PathDrawable : DrawablePath
{
    private PathDrawable(ISvgAssetLoader assetLoader, HashSet<Uri>? references)
        : base(assetLoader, references)
    {
    }

    public static PathDrawable Create(SvgPath svgPath, SKRect skViewport, DrawableBase? parent, ISvgAssetLoader assetLoader, HashSet<Uri>? references, DrawAttributes ignoreAttributes = DrawAttributes.None)
    {
        var drawable = new PathDrawable(assetLoader, references)
        {
            Element = svgPath,
            Parent = parent,
            IgnoreAttributes = ignoreAttributes
        };

        drawable.IsDrawable = drawable.CanDraw(svgPath, drawable.IgnoreAttributes) && drawable.HasFeatures(svgPath, drawable.IgnoreAttributes);

        if (!drawable.IsDrawable)
        {
            return drawable;
        }

        drawable.Path = svgPath.PathData?.ToPath(svgPath.FillRule);
        if (drawable.Path is null || drawable.Path.IsEmpty)
        {
            drawable.IsDrawable = false;
            return drawable;
        }

        drawable.Initialize(skViewport, references);

        return drawable;
    }

    private void Initialize(SKRect skViewport, HashSet<Uri>? references)
    {
        if (Element is not SvgPath svgPath || Path is null)
        {
            return;
        }

        IsAntialias = PaintingService.IsAntialias(svgPath);

        GeometryBounds = Path.Bounds;

        Transform = TransformsService.ToMatrix(svgPath.Transforms);

        var canDrawFill = true;
        var canDrawStroke = true;

        if (PaintingService.IsValidFill(svgPath))
        {
            Fill = PaintingService.GetFillPaint(svgPath, GeometryBounds, AssetLoader, references, IgnoreAttributes);
            if (Fill is null)
            {
                canDrawFill = false;
            }
        }

        if (PaintingService.IsValidStroke(svgPath, GeometryBounds))
        {
            Stroke = PaintingService.GetStrokePaint(svgPath, GeometryBounds, AssetLoader, references, IgnoreAttributes);
            if (Stroke is null)
            {
                canDrawStroke = false;
            }
        }

        if (canDrawFill && !canDrawStroke)
        {
            IsDrawable = false;
            return;
        }

        MarkerService.CreateMarkers(svgPath, Path, skViewport, this, AssetLoader, references);
    }
}

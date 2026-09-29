"""Deterministic chroma extraction/part packaging; no redrawing or content generation."""
from pathlib import Path
import importlib.util, json, uuid
import numpy as np
from PIL import Image
from collections import deque

ROOT=Path(__file__).resolve().parents[4]
HERE=Path(__file__).resolve().parent
OUT=ROOT/'Assets/Projects/MVP/Resources/MVP/Characters/Raccoon'
spec=importlib.util.spec_from_file_location('pipeline','/Users/daniel/.codex/skills/game-asset-pipeline/scripts/asset_pipeline.py')
pipeline=importlib.util.module_from_spec(spec);spec.loader.exec_module(pipeline)
parts={};report={}
for direction in ('Back','Diagonal','Side'):
    keyed=HERE/f'{direction}_RGBA.png'
    # Matte from magenta excess, preserving all non-key interior colors.
    rgb=np.array(Image.open(HERE/f'{direction}_Chroma.png').convert('RGB')).astype(float)
    excess=np.minimum(rgb[:,:,0],rgb[:,:,2])-rgb[:,:,1]
    a=np.where(excess>24,1-np.clip(excess/255,0,1),1)
    a=np.where(excess>223,0,a)
    unkey=np.clip((rgb-(1-a[:,:,None])*np.array([255,0,255]))/np.maximum(a[:,:,None],.001),0,255)
    rgba=np.dstack((unkey,np.round(a*255))).astype('uint8')
    rgba[a==0]=0
    Image.fromarray(rgba).save(keyed)
    report[direction]={'method':'magenta excess matte; interior RGB preserved','key':'#FF00FF'}
    im=Image.open(keyed).convert('RGBA'); arr=np.array(im)
    mask=arr[:,:,3]>8;objects=[]
    for sy,sx in zip(*np.nonzero(mask)):
        if not mask[sy,sx]:continue
        todo=deque([(int(sx),int(sy))]);mask[sy,sx]=False
        area=0;x0=x1=int(sx);y0=y1=int(sy)
        while todo:
            x,y=todo.popleft();area+=1;x0=min(x0,x);x1=max(x1,x);y0=min(y0,y);y1=max(y1,y)
            for nx,ny in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                if 0<=nx<im.width and 0<=ny<im.height and mask[ny,nx]:
                    mask[ny,nx]=False;todo.append((nx,ny))
        if area>=500:objects.append((area,(x0,y0,x1+1,y1+1)))
    if len(objects)!=4: raise RuntimeError((direction,'Expected four separated complete parts',objects))
    for area,bbox in objects:
        x0,y0,x1,y1=bbox;cx=(x0+x1)/2;cy=(y0+y1)/2
        name=('Head' if cx<im.width/2 else 'Body') if cy<im.height/2 else ('Weapon' if cx<im.width/2 else 'Tail')
        parts[direction,name]=im.crop(bbox)
        report[direction][name]={'source_bbox':bbox,'area':area}

# One common scale for each part across all directions; record registration instead of silently per-frame resizing.
for name in ('Head','Body','Tail','Weapon'):
    size=max(max(parts[d,name].size) for d in ('Back','Diagonal','Side'))
    scale=460/size
    for direction in ('Back','Diagonal','Side'):
        im=parts[direction,name];wh=tuple(round(v*scale) for v in im.size)
        canvas=Image.new('RGBA',(512,512));piece=im.resize(wh,Image.Resampling.LANCZOS)
        canvas.alpha_composite(piece,((512-wh[0])//2,(512-wh[1])//2))
        path=OUT/f'{direction}_{name}.png';canvas.save(path)
        a=np.array(canvas);alpha=a[:,:,3]>8
        spill=np.count_nonzero(alpha & (np.minimum(a[:,:,0].astype(int),a[:,:,2].astype(int))>a[:,:,1].astype(int)+24))
        if spill>alpha.sum()*.001:raise RuntimeError((path,'key spill',spill))
        report[direction][name].update({'common_scale':scale,'final_bbox':canvas.getchannel('A').point(lambda v:255 if v>8 else 0).getbbox(),'final_size':[512,512],'spill_pixels':int(spill)})
        meta=Path(str(path)+'.meta')
        if not meta.exists():
            meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nTextureImporter:\n  externalObjects: {}\n  serializedVersion: 13\n  mipmaps:\n    enableMipMap: 0\n    sRGBTexture: 1\n  isReadable: 0\n  textureSettings:\n    filterMode: 1\n    wrapU: 1\n    wrapV: 1\n  nPOTScale: 0\n  spriteMode: 1\n  spriteMeshType: 1\n  alignment: 0\n  spritePivot: {x: 0.5, y: 0.5}\n  spritePixelsToUnits: 256\n  alphaUsage: 1\n  alphaIsTransparency: 1\n  textureType: 8\n  textureShape: 1\n  maxTextureSize: 512\n  textureCompression: 0\n')
(HERE/'packaging_qa.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))

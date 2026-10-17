/** Native garment pattern edits. Numeric tailoring only; never reads personal PNGs. */
const sharp=require('sharp');
const fs=require('node:fs/promises');
const path=require('node:path');
const uri=b=>'data:image/png;base64,'+b.toString('base64');
module.exports=async function tailor(images,id,source){
 const side=id==='pelly';
 const svg=s=>Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512">${s}</svg>`);
 const render=s=>sharp(svg(s)).png().toBuffer();
 const torso=side?'M206,256 Q270,243 322,263 Q350,289 366,342 L369,385 Q369,424 336,432 Q278,447 221,430 Q181,424 183,382 L183,316 Q186,279 206,256Z':'M152,274 Q256,259 360,274 Q384,304 385,347 L380,405 Q380,452 349,464 Q308,474 280,463 Q255,455 231,464 Q195,475 161,463 Q130,452 130,405 L127,347 Q128,306 152,274Z';
 const sleeveR=side?'M307,272 Q336,261 361,284 Q397,315 418,355 Q428,378 410,390 Q390,403 368,390 Q337,367 319,337 Q304,311 307,272Z':'M353,286 Q393,292 417,335 Q435,369 435,397 Q425,419 393,413 Q370,399 366,370 Q358,331 353,286Z';
 const sleeveL=side?'M203,280 Q180,289 172,320 L155,365 Q150,390 173,397 Q194,402 204,378 L222,329Z':'M159,286 Q119,292 95,335 Q77,369 77,397 Q87,419 119,413 Q142,399 146,370 Q154,331 159,286Z';
 async function fabric(name){
  const raw=await fs.readFile(path.join(source,name+'.png'));const m=await sharp(raw).metadata();
  const box=name==='outerwear'?[.29,.35,.06,.15]:[.39,.4,.21,.25];
  const tile=await sharp(raw).extract({left:Math.floor(m.width*box[0]),top:Math.floor(m.height*box[1]),width:Math.floor(m.width*box[2]),height:Math.floor(m.height*box[3])}).resize(128,128,{fit:'fill'}).png().toBuffer();
  const variants=await Promise.all([sharp(tile).png().toBuffer(),sharp(tile).flop().png().toBuffer(),sharp(tile).flip().png().toBuffer(),sharp(tile).flop().flip().png().toBuffer()]);
  return sharp({create:{width:512,height:512,channels:4,background:'#d0d8ba'}}).composite(Array.from({length:16},(_,i)=>({input:variants[i%2+Math.floor(i/4)%2*2],left:i%4*128,top:Math.floor(i/4)*128}))).png().toBuffer();
 }
 function cloth(texture,d,extra=''){return `<defs><clipPath id="cloth"><path d="${d}"/></clipPath></defs><g clip-path="url(#cloth)"><path d="${d}" fill="#d0d8ba"/><image width="512" height="512" href="${uri(texture)}"/></g><path d="${d}" fill="none" stroke="#70503d" stroke-width="3.4" stroke-linejoin="round"/>${extra}`;}
 for(const name of ['top','outerwear']){
  const texture=await fabric(name);
  let seams=side?'<path d="M211,294Q229,332 218,408M330,288Q345,340 351,405" fill="none" stroke="#70503d" stroke-opacity=".25" stroke-width="3"/>':'<path d="M155,318Q150,365 155,421M357,318Q362,365 357,421" fill="none" stroke="#70503d" stroke-opacity=".25" stroke-width="3"/>';
  if(name==='outerwear')seams+=side?'<path d="M253,291L261,429" stroke="#f7e6d2" stroke-width="12"/><path d="M253,291L261,429" stroke="#70503d" stroke-width="2"/><circle cx="257" cy="335" r="4" fill="#b58d58"/><circle cx="259" cy="386" r="4" fill="#b58d58"/>':'<path d="M256,293V459" stroke="#f7e6d2" stroke-width="14"/><path d="M256,293V459" stroke="#70503d" stroke-width="2"/><circle cx="256" cy="339" r="4" fill="#b58d58"/><circle cx="256" cy="391" r="4" fill="#b58d58"/>';
  if(name==='outerwear')for(const x of side?[230]:[184,319])seams+=`<path d="M${x-26},370Q${x},377 ${x+26},370L${x+22},412Q${x},428 ${x-22},412Z" fill="#efa38b" stroke="#8f654d" stroke-width="2.5"/><path d="M${x-23},375Q${x},383 ${x+23},375" fill="none" stroke="#fff0d7" stroke-width="3"/>`;
  const hem=side?'M205,418Q268,443 352,419':'M150,449Q205,466 256,453Q308,467 365,449';
  images[name+'-body']=await render(cloth(texture,torso,seams+`<path d="${hem}" fill="none" stroke="#70503d" stroke-width="16" stroke-linecap="round"/><path d="${hem}" fill="none" stroke="#ead9b7" stroke-width="11" stroke-linecap="round"/>`));
  for(const [part,d]of[['left',sleeveL],['right',sleeveR]]){
   let cuff=side?(part==='right'?'M378,384Q397,396 415,378':'M157,380Q174,397 194,381'):(part==='right'?'M380,402Q407,421 431,401':'M81,401Q105,421 132,402');
   images[name+'-'+part]=await render(cloth(texture,d,`<path d="${cuff}" stroke="#70503d" stroke-width="15" fill="none" stroke-linecap="round"/><path d="${cuff}" stroke="#ead9b7" stroke-width="10" fill="none" stroke-linecap="round"/>`));
  }
  const collar=side?'M190,292 C212,290 225,285 238,274 C252,271 262,275 272,287 C286,299 302,299 315,292 L329,278 L338,286 L321,301 C303,310 285,309 267,296 C255,285 249,283 242,286 C223,300 207,302 190,303 Z':'M154,276 Q256,300 358,276 L363,290 Q256,321 149,290 Z';
  images[name+'-collar']=await render(`<path d="${collar}" fill="#ead9b7" stroke="#70503d" stroke-width="2.5"/><path d="${side?'M197,297Q221,294 240,280Q253,276 270,291Q293,309 316,298':'M155,285Q256,310 358,285'}" fill="none" stroke="#fff2d8" stroke-width="3"/>`);
  if(side)for(const part of ['left','right'])images[name+'-'+part]=await sharp({create:{width:512,height:512,channels:4,background:'#00000000'}}).png().toBuffer();
  images[name]=await sharp(images[name+'-body']).composite(['left','right','collar'].map(p=>({input:images[name+'-'+p]}))).png().toBuffer();
 }
 // A complete undershirt belongs under overalls; original baked satchels must not leak through.
 const neutral=await render(`<path d="${torso}" fill="#f4e6cc" stroke="#70503d" stroke-width="3"/>`);
 images['onePiece-body']=await sharp(neutral).composite([{input:images['onePiece-body']}]).png().toBuffer();
 // Preserve the painted denim and leather art while fitting independently sized feet.
 for(const [name,boxes]of Object.entries(side?{bottom:[[199,384,96,77],[287,387,87,83]],shoes:[[195,410,102,64],[272,414,110,64]]}:{bottom:[[141,391,115,89],[257,391,115,89]],shoes:[[147,439,103,59],[265,439,103,59]]})){
  const original=images[name];const meta=await sharp(original).metadata();
  // Split the already registered left/right artwork before sizing, retaining its seam and sole.
  const middle=side?296:256;
  for(const [i,part]of ['left','right'].entries()){
   let half=await sharp(original).extract({left:i?middle:0,top:0,width:i?512-middle:middle,height:meta.height}).png().toBuffer();
   const stats=await sharp(half).ensureAlpha().raw().toBuffer({resolveWithObject:true});let x0=stats.info.width,y0=512,x1=-1,y1=-1;
   for(let y=0;y<stats.info.height;y++)for(let x=0;x<stats.info.width;x++)if(stats.data[(y*stats.info.width+x)*4+3]>180){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);}
   if(x1<0)throw Error('Empty tailored '+name+' '+part);
   const [left,top,width,height]=boxes[i];half=await sharp(half).extract({left:x0,top:y0,width:x1-x0+1,height:y1-y0+1}).resize(width,height,{fit:'fill'}).png().toBuffer();
   images[name+'-'+part]=await sharp({create:{width:512,height:512,channels:4,background:'#00000000'}}).composite([{input:half,left,top}]).png().toBuffer();
  }
  if(side&&name==='shoes')for(const[part,x]of[['left',245],['right',303]]){const ankle=await render(`<path d="M${x},410Q${x+15},405 ${x+30},410L${x+28},453Q${x+15},461 ${x+2},453Z" fill="#fff1d8" stroke="#70503d" stroke-width="2.5"/>`);images[name+'-'+part]=await sharp(ankle).composite([{input:images[name+'-'+part]}]).png().toBuffer();}
  images[name]=await sharp(images[name+'-left']).composite([{input:images[name+'-right']}]).png().toBuffer();
 }
};

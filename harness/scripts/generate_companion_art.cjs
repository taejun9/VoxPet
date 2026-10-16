/** Slice generated transparent artwork and register it to each 512px character.
 * Development only: Node + sharp 0.35.5. Original imagegen outputs are immutable.
 * No personal character images are read by this generator.
 */
const fs = require('node:fs/promises');
const path = require('node:path');
const sharp = require('sharp');
const root = path.resolve(__dirname, '../..');
const source = path.join(root, 'harness/assets/companion-art');
const destination = process.argv[2];
if (!destination) throw new Error('Provide a new development output directory');
const profiles = {
  neulbo: {hat:[110,0,292,145],top:[88,282,342,170],outerwear:[87,280,344,175],onePiece:[143,284,230,192],bottom:[150,388,218,95],shoes:[158,450,208,53],cape:[12,209,488,260],backAccessory:[48,244,416,198],
    'hair-short':[66,29,388,300],'hair-long':[16,15,480,410],'hair-afro':[38,8,440,345],gun:[285,326,160,112],sword:[278,257,151,184],wand:[287,244,140,192],bow:[317,270,127,169]},
  pelly: {hat:[223,2,180,109],top:[183,241,220,176],outerwear:[182,241,224,178],onePiece:[203,241,185,199],bottom:[206,373,177,82],shoes:[224,423,157,59],cape:[58,170,442,269],backAccessory:[97,219,387,193],
    'hair-short':[168,30,247,246],'hair-long':[155,28,270,365],'hair-afro':[152,17,281,260],gun:[277,278,158,116],sword:[269,204,165,199],wand:[276,197,155,206],bow:[321,222,120,181]}
};
const ink='#70503d';
const defs=`<defs><linearGradient id="gold" x2=".7" y2="1"><stop stop-color="#fff1b8"/><stop offset=".45" stop-color="#edc36e"/><stop offset="1" stop-color="#ba8644"/></linearGradient><linearGradient id="pink" x2=".4" y2="1"><stop stop-color="#ffe4df"/><stop offset=".4" stop-color="#eaa4ad"/><stop offset="1" stop-color="#be728b"/></linearGradient><linearGradient id="mint" x2=".6" y2="1"><stop stop-color="#e8f0db"/><stop offset="1" stop-color="#90b3a2"/></linearGradient><radialGradient id="blush"><stop stop-color="#ed9b9f" stop-opacity=".65"/><stop offset="1" stop-color="#ed9b9f" stop-opacity="0"/></radialGradient></defs>`;
const star=(x,y,s)=>`<path transform="translate(${x} ${y}) scale(${s})" d="M0,-10 Q2,-9 3,-3 L9,-2 Q11,0 6,3 L7,9 Q6,11 0,6 L-6,9 Q-9,10 -6,3 L-10,-1 Q-10,-3 -3,-3 Z" fill="url(#gold)" stroke="${ink}" stroke-width=".65"/><path d="M${x-s*2},${y-s*5}l${s*2},${-s*2}" stroke="#fff8df" stroke-width="${s}" stroke-linecap="round"/>`;
function svg(content){return Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="1024" viewBox="0 0 512 512">${defs}${content}</svg>`);}
function small(kind,id) {
  const side=id==='pelly';
  if(kind==='glasses') {
    const eyes=side?[[252,153]]:[[194,197],[322,197]];
    return svg(eyes.map(([x,y])=>`<ellipse cx="${x}" cy="${y}" rx="39" ry="33" fill="#cce7e8" fill-opacity=".18" stroke="#a98151" stroke-width="5"/><path d="M${x-20},${y-12}q10,-15 24,-9" stroke="#fff5dc" stroke-width="3" opacity=".8" fill="none"/>`).join('')+(side?'<path d="M291,148l62,-12" stroke="#a98151" stroke-width="4"/>':'<path d="M233,195q25,-13 50,0M155,190l-34,-11M361,190l34,-11" stroke="#a98151" stroke-width="4" fill="none"/>'));
  }
  if(kind==='faceDecoration')return svg((side?[[297,182]]:[[153,237],[364,237]]).map(([x,y])=>`<ellipse cx="${x}" cy="${y}" rx="29" ry="19" fill="url(#blush)"/><path d="M${x-12},${y-2}l-3,7m12,-6l-3,7" stroke="#d68a8f" stroke-width="2" stroke-linecap="round" opacity=".6"/>`).join(''));
  if(kind==='earrings')return svg((side?[[374,217]]:[[109,249],[407,249]]).map(([x,y])=>`<circle cx="${x}" cy="${y}" r="4" fill="url(#gold)" stroke="${ink}" stroke-width="1"/><path d="M${x},${y+4}v13" stroke="#bc9455" stroke-width="2"/>${star(x,y+28,1.2)}`).join(''));
  if(kind==='headAccessory') {
    const x=side?338:256,y=side?90:82;
    return svg(`<g transform="translate(${x} ${y})"><path d="M-4,1C-24,-32 -66,-33 -59,-7C-70,21 -29,32 -3,7M4,1C24,-32 66,-33 59,-7C70,21 29,32 3,7" fill="url(#pink)" stroke="${ink}" stroke-width="2"/><path d="M-5,4l-34,46 29,-7 9,-26 15,29 25,3L5,4" fill="url(#pink)" stroke="${ink}" stroke-width="2"/><path d="M-49,-10q27,1 40,13M49,-10Q22,-9 8,3" stroke="#fff0e1" fill="none" opacity=".7" stroke-width="3"/><ellipse rx="11" ry="14" fill="url(#pink)" stroke="${ink}" stroke-width="2"/></g>`);
  }
  if(kind==='handAccessory') {
    const x=side?405:405,y=side?369:409;
    return svg(`<ellipse cx="${x}" cy="${y}" rx="25" ry="10" fill="none" stroke="#b8874e" stroke-width="9"/><ellipse cx="${x}" cy="${y-2}" rx="25" ry="10" fill="none" stroke="url(#gold)" stroke-width="5"/>${star(x,y+13,1.1)}`);
  }
  if(kind==='effect')return svg([[74,155,1.6],[437,120,1.9],[98,403,1.2],[447,440,1.2]].map(([x,y,s])=>star(x,y,s)).join(''));
  if(kind==='heart')return svg(`<path d="M256,360C212,331 185,307 198,284C211,259 244,265 256,287C270,260 304,262 315,285C328,312 296,339 256,360Z" fill="url(#pink)" stroke="#a56b71" stroke-width="2"/><path d="M212,289q10,-13 23,-4" fill="none" stroke="#fff3e7" stroke-width="5" stroke-linecap="round"/>${star(319,267,.8)}${star(189,327,.6)}`);
  if(kind==='claw') {
    const x=side?377:365,y=side?290:327;
    return svg(`<g transform="translate(${x} ${y})"><path d="M18,91L14,67Q4,63 5,51Q6,42 15,44L23,48V13Q23,3 31,3Q40,3 40,13V43Q44,37 51,40Q57,34 64,40Q73,38 77,46L79,68Q78,83 64,93L62,104H24Z" fill="#f5dfbd" stroke="#70503d" stroke-width="3.5" stroke-linejoin="round"/><path d="M24,48v15m16,-20v15m14,-14v15m12,-14v13M20,74q15,8 30,5" fill="none" stroke="#b18d69" stroke-width="2" stroke-linecap="round"/><path d="M28,15v22M29,69q13,6 27,1" stroke="#fff7e4" stroke-width="3" stroke-linecap="round"/><path d="M19,95q22,7 48,-1l-2,14q-24,7 -44,0Z" fill="url(#mint)" stroke="#70503d" stroke-width="2.5"/></g>`);
  }
  if(kind==='bow') {
    const [x,y,w,h]=profiles[id].bow;
    return svg(`<g transform="translate(${x} ${y}) scale(${w/120} ${h/170})"><path d="M42,3Q115,85 42,167" fill="none" stroke="${ink}" stroke-width="8"/><path d="M42,3Q112,85 42,167" fill="none" stroke="url(#gold)" stroke-width="5"/><path d="M42,3L25,85 42,167" stroke="#eee0c3" stroke-width="2" fill="none"/><path d="M12,85h102" stroke="#916848" stroke-width="4"/><path d="M114,85l-15,-8v16Z" fill="#d5e7dd" stroke="${ink}" stroke-width="1"/><path d="M15,85l-12,-10m12,10l-12,10" stroke="#aaa3b8" stroke-width="5"/></g>`);
  }
  throw new Error(kind);
}
async function trimmed(buffer) {
  const {data,info}=await sharp(buffer).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let x0=info.width,y0=info.height,x1=0,y1=0;
  for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>32){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);}
  if(x0>x1)throw new Error('Empty asset');
  return sharp(buffer).extract({left:x0,top:y0,width:x1-x0+1,height:y1-y0+1}).png().toBuffer();
}
async function placed(buffer,rect) {
  const [left,top,width,height]=rect;
  const input=await sharp(await trimmed(buffer)).resize(width,height,{fit:'fill'}).png().toBuffer();
  return sharp({create:{width:512,height:512,channels:4,background:'#00000000'}}).composite([{input,left,top}]).png().toBuffer();
}
async function frame(kind,index,columns,rows) {
  const p=path.join(source,(kind==='cape'?'cape-v2':kind)+'.png');const m=await sharp(p).metadata();const col=index%columns,row=Math.floor(index/columns);
  // Each painted pose has its own bounds: the lower row's tips cross the
  // upper row's column gutters. A single grid leaks neighboring fragments.
  if(kind==='cape') {
    const [x,y,right,bottom]=[[0,130,477,460],[480,120,1030,375],[1030,40,1536,425],[55,490,440,950],[510,510,1020,885],[1035,620,1536,880]][index];
    const left=Math.round(x*m.width/1536),top=Math.round(y*m.height/1024);
    return sharp(p).extract({left,top,width:Math.round(right*m.width/1536)-left,height:Math.round(bottom*m.height/1024)-top}).png().toBuffer();
  }
  const xs=Array.from({length:columns+1},(_,i)=>i/columns);
  const ys=Array.from({length:rows+1},(_,i)=>i/rows);
  const left=Math.round(xs[col]*m.width),top=Math.round(ys[row]*m.height),right=Math.round(xs[col+1]*m.width),bottom=Math.round(ys[row+1]*m.height);
  return sharp(p).extract({left,top,width:right-left,height:bottom-top}).png().toBuffer();
}
(async()=>{
  await fs.mkdir(destination,{recursive:true});
  for(const id of Object.keys(profiles)){
    const out=path.join(destination,id);await fs.mkdir(out,{recursive:true});const images={};
    for(const kind of ['top','outerwear','onePiece','bottom','shoes','hair-short','hair-long','hair-afro']) images[kind]=await placed(await fs.readFile(path.join(source,(kind==='hair-long'?'hair-long-v2':kind)+'.png')),profiles[id][kind]);
    for(const [kind,count,columns,rows] of [['hat',8,4,2],['cape',6,3,2]])for(let i=0;i<count;i++)images[kind+(i?'-'+i:'')]=await placed(await frame(kind,i,columns,rows),profiles[id][kind]);
    for(const kind of ['glasses','faceDecoration','earrings','headAccessory','handAccessory','effect','heart','claw','bow'])images[kind]=await sharp(small(kind,id)).resize(512,512).png().toBuffer();
    // Split clothing inside the fabric, with matching shoulder anchors. Back
    // sleeves sit behind the torso; the front sleeve sits over the front arm.
    const split=async(name,part,mask)=>{
      images[name+'-'+part]=await sharp(images[name]).composite([{input:Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512">${mask}</svg>`),blend:'dest-in'}]).png().toBuffer();
    };
    for(const name of ['top','outerwear','onePiece']) {
      const left=id==='pelly'?0:153,right=id==='pelly'?334:361;
      await split(name,'body',`<rect x="${left-22}" y="0" width="${right-left+44}" height="512" fill="white"/>`);
      await split(name,'left',`<rect width="${left}" height="512" fill="white"/>`);
      await split(name,'right',`<rect x="${right}" width="${512-right}" height="512" fill="white"/>`);
    }
    for(const name of ['shoes','bottom']) {
      const middle=id==='pelly'?310:256;
      await split(name,'left',`<rect width="${middle}" height="512" fill="white"/>`);
      await split(name,'right',`<rect x="${middle}" width="${512-middle}" height="512" fill="white"/>`);
    }
    images['backAccessory']=await placed(await frame('cape',1,3,2),profiles[id].backAccessory);
    for(const [kind,box]of[['toy-gun','gun'],['toy-sword','sword'],['magic-wand','wand']])images[kind]=await placed(await fs.readFile(path.join(source,(kind==='hair-long'?'hair-long-v2':kind)+'.png')),profiles[id][box]);
    for(const kind of ['hair-short','hair-long','hair-afro']) {
      // Preserve each painted curl's outline. The side-view face and bill stay in front
      // of the back locks; never crop hair with a visible horizontal cutoff.
      const faceWindow=Buffer.from('<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512"><ellipse cx="300" cy="187" rx="100" ry="88" fill="white"/><ellipse cx="181" cy="206" rx="110" ry="88" fill="white"/></svg>');
      images[kind+'-fringe']=id==='pelly'?await sharp(images[kind]).composite([{input:faceWindow,blend:'dest-out'}]).png().toBuffer():images[kind];
    }
    for(const[name,bytes]of Object.entries(images))await fs.writeFile(path.join(out,name+'.png'),bytes);
    await sharp({create:{width:1,height:1,channels:4,background:'#00000000'}}).png().toFile(path.join(out,'empty.png'));
    for(const kind of ['hat','glasses','faceDecoration','earrings','top','bottom','onePiece','shoes','outerwear','cape','headAccessory','backAccessory','handAccessory','effect','hair-short','hair-long','hair-afro']){
      const item=await trimmed(images[kind]);await sharp(item).resize(160,160,{fit:'contain',background:'#00000000'}).png().toFile(path.join(out,'thumb-'+kind+'.png'));
    }
  }
  console.log('Registered transparent art for neulbo and pelly; original generated assets preserved');
})().catch(e=>{console.error(e);process.exitCode=1;});

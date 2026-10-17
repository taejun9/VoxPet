/** Calibrated prop handles, closed grips and existing code-native effects. */
const fs=require('node:fs/promises'),path=require('node:path'),sharp=require('sharp');
module.exports=async(images,id,source,trimmed)=>{
 const side=id==='pelly',hand=side?[415,385]:[402,422],leftHand=side?[220,410]:[110,422];
 const svg=s=>Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512">${s}</svg>`);
 async function prop(kind,file,w,h,gx,gy,origin=hand){
  const painted=await sharp(await trimmed(await fs.readFile(path.join(source,file+'.png')))).resize(w,h,{fit:'fill'}).png().toBuffer();
  images[kind]=await sharp({create:{width:512,height:512,channels:4,background:'#00000000'}}).composite([{input:painted,left:Math.round(origin[0]-gx*w),top:Math.round(origin[1]-gy*h)}]).png().toBuffer();
 }
 await prop('pistol','pistol-v3',130,80,.80,.66);
 await prop('toy-gun','toy-gun-v3',128,83,.83,.73);
 await prop('toy-sword','toy-sword-v3',47,190,.50,.84);
 await prop('one-hand','one-hand-v3',48,202,.50,.84);
 await prop('two-hand','two-hand-v3',51,235,.50,.77);
 await prop('magic-wand','magic-wand-v3',29,185,.50,.85);
 await prop('bow','bow-v4',75,214,.13,.50,leftHand);
 for(const[part,[x,y]]of[['left',leftHand],['right',hand]]){
  // Three short claws/feather folds curl in front of the handle; the gap between
  // palm and fingers remains transparent so the grip stays visible.
  const fill=side?'#a4927e':'#cfaa8b',edge='#482919';
  const fingers=side?`<path d="M-19,-12Q-29,-4 -19,8L-6,13Q2,10 -1,3L-13,-1Q2,5 6,-1Q9,-8 0,-10L-10,-14Z" fill="${fill}" stroke="${edge}" stroke-width="3"/><path d="M-17,-3Q-12,1 -5,1M-18,4L-8,7" fill="none" stroke="#efe5d5" stroke-width="3" stroke-linecap="round"/>`:`<path d="M-20,-13Q-28,-5 -20,10Q-12,17 2,11L8,3Q10,-5 1,-5L-11,-3Q0,-2 3,-8Q5,-14 -3,-16Z" fill="${fill}" stroke="${edge}" stroke-width="3"/><path d="M-19,1Q-16,9 -11,8L-12,0M-10,1Q-7,10 -3,7L-4,-1" fill="#fff9ed" stroke="${edge}" stroke-width="2.5" stroke-linejoin="round"/>`;
  images['grip-back-'+part]=await sharp(svg(`<g transform="translate(${x} ${y})"><path d="M-24,-19Q-31,-8 -25,9Q-20,26 0,25Q24,23 28,7Q30,-10 18,-19Q0,-28 -24,-19Z" fill="${fill}" stroke="${edge}" stroke-width="3"/><path d="M-17,-14Q-7,-21 9,-15" fill="none" stroke="${side?'#efe5d5':'#e5c6a5'}" stroke-width="4" stroke-linecap="round"/></g>`)).png().toBuffer();
  images['grip-'+part]=await sharp(svg(`<g transform="translate(${x} ${y})">${fingers}</g>`)).png().toBuffer();
 }
 const fill=side?'#a4927e':'#cfaa8b';
 images['heart']=await sharp(svg(`<g transform="translate(${hand[0]} ${hand[1]})"><path d="M-20,18Q-32,6 -24,-10L-10,-8 -11,-31Q-11,-41 -3,-41Q5,-40 6,-31L8,-13 18,-24Q26,-31 31,-22Q36,-15 28,-8L16,5Q14,22 -1,24Z" fill="${fill}" stroke="#482919" stroke-width="3" stroke-linejoin="round"/><path d="M-7,-33L-5,-39 1,-37 2,-31M23,-23L29,-22 28,-17" fill="#fff8eb" stroke="#70503d" stroke-width="1.8"/><path d="M-15,2Q-8,9 1,5M-13,10Q-6,17 5,12M8,-13L16,5" fill="none" stroke="#88674f" stroke-width="2" stroke-linecap="round"/><path d="M8,-48C-7,-62 -17,-50 -7,-40L8,-28 23,-41C34,-52 20,-63 8,-48Z" fill="#e8a1af" stroke="#a9697a" stroke-width="2"/><path d="M-4,-49Q0,-54 5,-49" fill="none" stroke="#fff3ed" stroke-width="2.5" stroke-linecap="round"/></g>`)).png().toBuffer();
 images['muzzle-flash']=await sharp(svg(`<g transform="translate(${hand[0]-107} ${hand[1]-38})"><path d="M0,0L-17,-6 -13,-18 -28,-9 -37,-15 -30,0 -42,9 -25,8 -17,20 -16,6Z" fill="#ffda6e" stroke="#e8a246" stroke-width="2"/><path d="M-4,0L-23,-5 -18,0 -25,5Z" fill="#fff8d0"/></g>`)).png().toBuffer();
 images['cast-tip']=await sharp(svg(`<g transform="translate(${hand[0]} ${hand[1]-157})"><path d="M0,-20L5,-5 20,0 5,5 0,20 -5,5 -20,0 -5,-5Z" fill="#d3f4ff" stroke="#76b4d2" stroke-width="2"/><circle r="5" fill="#fffdf0"/></g>`)).png().toBuffer();
 images['bow-string']=await sharp(svg('<path d="M0,1L512,1" stroke="#c7ac7d" stroke-width="2"/>')).png().toBuffer();
 images['arrow']=await sharp(svg('<path d="M38,10H512" stroke="#815a36" stroke-width="4"/><path d="M0,10L45,0 37,10 45,20Z" fill="#bfced0" stroke="#5f5140" stroke-width="2"/><path d="M480,10L507,0M480,10L507,20" stroke="#b89056" stroke-width="5"/>')).png().toBuffer();
};

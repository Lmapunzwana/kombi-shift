from pathlib import Path
import re,json,html
from reportlab.pdfgen import canvas
from reportlab.platypus import SimpleDocTemplate,Paragraph,Spacer,Table,TableStyle,PageBreak,Image,KeepTogether,Flowable
from reportlab.lib import colors
from reportlab.lib.styles import getSampleStyleSheet,ParagraphStyle
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from pypdf import PdfReader
R=Path(__file__).resolve().parents[1]
E=R/'Docs/Validation/Expansion';D=R/'Docs/Assessment';OUT=R/'output/pdf/KombiShift2_Expansion_Report_2026-11-10.pdf'
font=Path('/usr/share/fonts/truetype/dejavu')
if (font/'DejaVuSans.ttf').exists():
 pdfmetrics.registerFont(TTFont('Body',str(font/'DejaVuSans.ttf')));pdfmetrics.registerFont(TTFont('Bold',str(font/'DejaVuSans-Bold.ttf')))
else:
 from reportlab.pdfbase.pdfmetrics import Font
 pdfmetrics.registerFont(Font('Body','Helvetica','WinAnsiEncoding'));pdfmetrics.registerFont(Font('Bold','Helvetica-Bold','WinAnsiEncoding'))
pdfmetrics.registerFontFamily('Body',normal='Body',bold='Bold',italic='Body',boldItalic='Bold')
INK=colors.HexColor('#152d35');TEAL=colors.HexColor('#147d87');GOLD=colors.HexColor('#dda135');MUTED=colors.HexColor('#53666d');PALE=colors.HexColor('#edf4f3')
W,H=A4;CW=W-88
styles=getSampleStyleSheet()
styles.add(ParagraphStyle(name='Text',fontName='Body',fontSize=9.5,leading=14,textColor=INK,spaceAfter=7))
styles.add(ParagraphStyle(name='Section',fontName='Bold',fontSize=15,leading=20,textColor=TEAL,spaceBefore=17,spaceAfter=8,keepWithNext=True))
styles.add(ParagraphStyle(name='Chapter',fontName='Bold',fontSize=23,leading=29,textColor=INK,spaceAfter=17,keepWithNext=True))
styles.add(ParagraphStyle(name='SmallText',fontName='Body',fontSize=8,leading=11,textColor=MUTED,spaceAfter=7))
styles.add(ParagraphStyle(name='Cell',fontName='Body',fontSize=8,leading=11,textColor=INK))
styles.add(ParagraphStyle(name='CellHead',fontName='Bold',fontSize=8,leading=11,textColor=colors.white))
def clean(s):
 return s.replace('\u2011','-').replace('—','-').replace('–','-').replace('→','->').replace('×','x').replace('’',"'").replace('‘',"'").replace('“','"').replace('”','"')
def mark(s):
 s=html.escape(clean(s));s=re.sub(r'\*\*(.*?)\*\*',r'<b>\1</b>',s);s=re.sub(r'`([^`]*)`',r'<font color="#147d87">\1</font>',s)
 s=re.sub(r'\[([^]]+)\]\(([^)]+)\)',r'\1',s);return s
styles['Text'].allowWidows=0
styles['Text'].allowOrphans=0
P=lambda text,style='Text':Paragraph(mark(text),styles[style])
def table(rows,widths=None):
 n=len(rows[0]);widths=widths or [CW/n]*n
 data=[[Paragraph(mark(cell),styles['CellHead' if i==0 else 'Cell']) for cell in row] for i,row in enumerate(rows)]
 t=Table(data,colWidths=widths,repeatRows=1,hAlign='LEFT')
 t.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,0),TEAL),('VALIGN',(0,0),(-1,-1),'TOP'),('LEFTPADDING',(0,0),(-1,-1),7),('RIGHTPADDING',(0,0),(-1,-1),7),('TOPPADDING',(0,0),(-1,-1),7),('BOTTOMPADDING',(0,0),(-1,-1),7),('ROWBACKGROUNDS',(0,1),(-1,-1),[PALE,colors.white]),('LINEBELOW',(0,0),(-1,0),1,TEAL),('LINEBELOW',(0,1),(-1,-1),.25,colors.HexColor('#d9e4e4'))]))
 return t
class Footer(canvas.Canvas):
 def __init__(self,*a,**k):super().__init__(*a,**k);self.states=[]
 def showPage(self):self.states.append(dict(self.__dict__));self._startPage()
 def save(self):
  for state in self.states:
   self.__dict__.update(state);self.setStrokeColor(GOLD);self.setLineWidth(.7);self.line(44,38,W-44,38);self.setFont('Body',7);self.setFillColor(MUTED);self.drawString(44,25,'KOMBI SHIFT 2  |  GROUP 6  |  10 NOV 2026');self.drawRightString(W-44,25,f'{self._pageNumber} / {len(self.states)}');super().showPage()
  super().save()
class TripFlow(Flowable):
 def __init__(self):super().__init__();self.width=CW;self.height=118
 def draw(self):
  c=self.canv;labels=['ONBOARD','REQUESTED','ALIGHTING','COMPLETED'];bw=108;gap=(CW-4*bw)/3
  for i,label in enumerate(labels):
   x=i*(bw+gap);c.setFillColor(PALE);c.setStrokeColor(TEAL);c.roundRect(x,70,bw,31,5,stroke=1,fill=1);c.setFont('Bold',8);c.setFillColor(INK);c.drawCentredString(x+bw/2,82,label)
   if i<3:
    c.setStrokeColor(TEAL);c.line(x+bw,85,x+bw+gap,85);c.line(x+bw+gap-4,89,x+bw+gap,85);c.line(x+bw+gap-4,81,x+bw+gap,85)
  c.setFont('Body',8);c.setFillColor(MUTED);c.drawString(0,47,'Real seat arrival     ->     stop bell / text     ->     stopped service     ->     fare + XP')
  c.setStrokeColor(GOLD);x=bw+gap+bw/2;c.line(x,70,x,20);c.line(x,20,2*(bw+gap)+bw/2,20);c.line(2*(bw+gap)+bw/2,20,2*(bw+gap)+bw/2,70);c.setFont('Bold',8);c.setFillColor(INK);c.drawString(x+8,30,'MISSED: one penalty, then a valid recovery stop')
def md(text):
 out=[];lines=text.splitlines();i=0
 while i<len(lines):
  line=lines[i].strip()
  if not line:i+=1;continue
  if line.startswith('|'):
   rows=[]
   while i<len(lines) and lines[i].strip().startswith('|'):
    row=[x.strip() for x in lines[i].strip().strip('|').split('|')]
    if not all(re.fullmatch(r':?-+:?',x.replace(' ','')) for x in row):rows.append(row)
    i+=1
   if rows:
    n=len(rows[0]);widths=([81,111,CW-192] if n==3 else [CW/n]*n)
    out.append(table(rows,widths));out.append(Spacer(1,10))
   continue
  if line.startswith('#'):
   heading=line.lstrip('#').strip()
   if not line.startswith('##'):out.append(P(heading,'Chapter'))
   else:out.append(P(heading,'Section'))
   i+=1;continue
  if re.match(r'^[-*] ',line) or re.match(r'^\d+[.] ',line):out.append(P(line));i+=1;continue
  buf=[line];i+=1
  while i<len(lines) and lines[i].strip() and not lines[i].startswith(('#','|','- ')) and not re.match(r'^\d+[.] ',lines[i]):buf.append(lines[i].strip());i+=1
  out.append(P(' '.join(buf)))
 return out
story=[]
story+=[Spacer(1,35),P('UNIVERSITY OF ZIMBABWE  /  HAI311','SmallText'),Spacer(1,14),P('KOMBI SHIFT 2','Chapter'),P('Service, competition and route expansion','Section'),P('Current design and engineering evidence - 10 November 2026'),Spacer(1,10)]
metrics=json.loads((E/'map-metrics.json').read_text())
story+=[table([['CONNECTED ITINERARY','SERVICE LOCATIONS','VALIDATION SCOPE'],[f"{metrics['expanded']['lengthMetres']/1000:.2f} km",'7 stops / 2 stages','Automated + platform checks; human testing remains']], [CW*.3,CW*.3,CW*.4]),Spacer(1,17)]
img=E/'Visual/01-live-menu.png'
if img.exists():
 from PIL import Image as PILImage
 iw,ih=PILImage.open(img).size;story.append(Image(str(img),width=CW,height=CW*ih/iw));story.append(Spacer(1,8))
story+=[P('The existing Unity game is extended in place. This report separates implemented features, executed technical evidence and work that still requires people. It does not claim a complete rubric score or fabricated playtest results.'),P('Unity 6000.6.2f1  |  Branch feat/kombishift2-unity6  |  Assessment deadline 13 November 2026','SmallText'),PageBreak()]
story+=[P('1 / Current design','Chapter')]+md((D/'CURRENT_DESIGN.md').read_text().split('\n',1)[1])
story+=[Spacer(1,12),P('2 / Passenger service flow','Chapter'),TripFlow(),P('Character movement states remain separate from the trip data above. A fare is paid after a real alighting completion, and a missed request is penalised once. Rival queues use the same station ownership and physical boarding callbacks.'),P('Rendered scene evidence','Section')]
img=E/'Visual/grounded-final-start.png'
if not img.exists():img=E/'Visual/05-chase-view.png'
if img.exists():
 from PIL import Image as PILImage
 iw,ih=PILImage.open(img).size;story.append(Image(str(img),width=CW,height=CW*ih/iw));story.append(Spacer(1,8));story.append(P('Actual Editor capture from a staged verification fixture. Background agents may be isolated; this is not a human playtest.','SmallText'))
story+=[PageBreak(),P('3 / Engineering report','Chapter')]+md((D/'EXPANSION_ENGINEERING_REPORT.md').read_text().split('\n',1)[1])
story+=[PageBreak(),P('4 / Panel practice','Chapter')]+md((D/'EXPANSION_DEFENCE.md').read_text().split('\n',1)[1])
story+=[P('Evidence and source locations','Section'),P('Structured results, scene audits, source hashes and build identities: Docs/Validation/Expansion. Changed files: changed-files.txt. Reproduction and authentic tester tasks: Docs/Assessment/EXPANSION_TEST_PLAN.md. Existing PDF/deck/submission snapshots are preserved as earlier-version material.'),P('AI and assets','Section'),P('OpenAI Codex assisted implementation, technical verification and report preparation at Jayden\'s request. Imported Unity/RCC/city/vehicle/character assets remain attributed to their existing sources. Members must record actual understanding and contributions. Authentic feedback, member exercises, assigned-book reading and source redistribution permission are not asserted by this report.')]
OUT.parent.mkdir(parents=True,exist_ok=True)
doc=SimpleDocTemplate(str(OUT),pagesize=A4,rightMargin=44,leftMargin=44,topMargin=43,bottomMargin=52,title='Kombi Shift 2 - Expansion design and engineering report',author='Group 6 / AI-assisted technical preparation')
doc.build(story,canvasmaker=Footer)
reader=PdfReader(OUT);print(OUT);print('Pages:',len(reader.pages));print('Text characters:',sum(len(p.extract_text() or '') for p in reader.pages))

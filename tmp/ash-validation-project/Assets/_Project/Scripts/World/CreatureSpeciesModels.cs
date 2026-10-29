using UnityEngine;
using PokeMoba.Data;

namespace PokeMoba.World
{
    public static partial class CreatureModelBuilder
    {
        // Species silhouettes take precedence over generic type decorations.
        static bool BuildRecognizableSpecies(SpeciesData s, Parts p)
        {
            switch (s.DexNumber)
            {
                case 1: case 2: case 3: SeedCreature(p, s.DexNumber); break;
                case 4: case 5: case 6: FireLizard(p, s.DexNumber); break;
                case 7: case 8: case 9: Turtle(p, s.DexNumber); break;
                case 25: ElectricMouse(p); break;
                case 39: PinkSinger(p); break;
                case 94: ShadowCreature(p); break;
                case 133: Fox(p); break;
                case 143: Sleeper(p); break;
                default: return false;
            }
            return true;
        }

        static readonly Color Cream = new Color(1f, .89f, .66f);
        static readonly Color Ink = new Color(.065f, .045f, .055f);
        static Transform Oval(Parts p, string name, float x, float y, float z,
            float sx, float sy, float sz, Color color, Vector3 rotation = default)
            => p.Add(PrimitiveType.Sphere, name, new Vector3(x,y,z), new Vector3(sx,sy,sz), color, rotation);

        // Reparent while preserving the authored world pose, so all details follow the animated part.
        static void Attach(Transform detail, Transform bone) => detail.SetParent(bone, true);

        static void Face(Parts p, Vector3 center, float spacing, Color iris)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var at = center + Vector3.right * spacing * side;
                Attach(p.Add(PrimitiveType.Sphere, "Esclerótica", at, new Vector3(.22f,.28f,.075f), Color.white), p.Head);
                Attach(p.Add(PrimitiveType.Sphere, "Iris", at + new Vector3(0,-.015f,.039f), new Vector3(.145f,.205f,.035f), iris), p.Head);
                Attach(p.Add(PrimitiveType.Sphere, "Pupila", at + new Vector3(0,-.012f,.058f), new Vector3(.078f,.15f,.025f), Ink), p.Head);
                Attach(p.Add(PrimitiveType.Sphere, "Reflejo ocular", at + new Vector3(-.025f,.05f,.073f), Vector3.one * .045f, Color.white), p.Head);
            }
        }

        static void Feet(Parts p, Color color, bool four = false)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                int count = four ? 2 : 1;
                for (int i = 0; i < count; i++)
                {
                    float z = four ? -.38f + i * .85f : .15f;
                    var foot = Oval(p,"Pata", side * .34f,.22f,z,.33f,.4f,.5f,color);
                    p.Legs.Add(foot);
                    for (int toe = -1; toe <= 1; toe++)
                        Attach(Oval(p,"Garra",side*.34f+toe*.08f,.12f,z+.23f,.065f,.075f,.13f,Cream),foot);
                }
            }
        }

        static void Ear(Parts p, float side, Vector3 at, Color color, Color inner, float length = .7f)
        {
            var ear = p.Add(PrimitiveType.Sphere,"Oreja puntiaguda",at,new Vector3(.22f,length,.17f),color,new Vector3(0,0,-side*22));
            var inset = p.Add(PrimitiveType.Sphere,"Interior de oreja",at+new Vector3(0,.015f,.07f),new Vector3(.12f,length*.72f,.05f),inner,new Vector3(0,0,-side*22));
            Attach(inset,ear);
            Attach(ear,p.Head);
        }

        static void ElectricMouse(Parts p)
        {
            Color yellow=p.Main, brown=new Color(.43f,.23f,.1f);
            p.Body=Oval(p,"Cuerpo de Pikachu",0,.78f,0,.9f,1.2f,.7f,yellow);
            p.Head=Oval(p,"Cabeza ancha",0,1.5f,.1f,1.05f,.86f,.78f,yellow);
            Face(p,new Vector3(0,1.58f,.475f),.235f,Ink);
            for(int side=-1;side<=1;side+=2)
            {
                Ear(p,side,new Vector3(side*.38f,2.04f,.04f),yellow, yellow,.85f);
                Attach(Oval(p,"Punta negra",side*.53f,2.39f,.04f,.19f,.3f,.17f,Ink,new Vector3(0,0,-side*22)),p.Head);
                Attach(Oval(p,"Mejilla roja",side*.405f,1.34f,.405f,.25f,.23f,.07f,new Color(.94f,.13f,.08f)),p.Head);
                Oval(p,"Brazo",side*.43f,.86f,.2f,.23f,.49f,.25f,yellow,new Vector3(0,0,side*24));
                Oval(p,"Raya dorsal",0,.66f+(.19f*(side+1)), -.342f,.64f,.105f,.045f,brown);
            }
            Attach(Oval(p,"Nariz",0,1.44f,.508f,.075f,.05f,.045f,Ink),p.Head);
            Attach(Oval(p,"Sonrisa",0,1.28f,.47f,.2f,.035f,.025f,brown),p.Head);
            Feet(p,yellow);
            p.Tail=new GameObject("Cola de rayo articulada").transform;
            p.Tail.SetParent(p.Model,false); p.Tail.localPosition=new Vector3(0,.55f,-.36f)*p.Size;
            // Connected angular segments make the lightning silhouette readable from either side.
            Vector3[] points={new Vector3(0,.55f,-.36f),new Vector3(.2f,.85f,-.62f),new Vector3(.05f,1.06f,-.72f),new Vector3(.46f,1.39f,-.8f),new Vector3(.28f,1.75f,-.83f)};
            for(int i=0;i<points.Length-1;i++)
            {
                var d=points[i+1]-points[i];
                var piece=p.Add(PrimitiveType.Cube,"Tramo de rayo",(points[i]+points[i+1])*.5f,new Vector3(i==0?.15f:.29f,d.magnitude+.09f,.12f),i==0?brown:yellow);
                piece.localRotation=Quaternion.FromToRotation(Vector3.up,d);
                Attach(piece,p.Tail);
            }
        }

        static void SeedCreature(Parts p,int dex)
        {
            Color skin=p.Main, spot=new Color(.08f,.4f,.32f), leaf=new Color(.15f,.49f,.23f);
            p.Body=Oval(p,"Cuerpo robusto",0,.65f,-.12f,1.05f,.85f,1.35f,skin);
            p.Head=Oval(p,"Cabeza ancha",0,.93f,.68f,1.15f,.85f,.9f,skin);
            Feet(p,skin,true); Face(p,new Vector3(0,1.04f,1.09f),.3f,new Color(.75f,.12f,.18f));
            for(int side=-1;side<=1;side+=2)
            {
                Ear(p,side,new Vector3(side*.43f,1.32f,.56f),skin,spot,.4f);
                Attach(Oval(p,"Mancha de frente",side*.27f,1.23f,.97f,.19f,.14f,.035f,spot),p.Head);
                Oval(p,"Mancha lateral",side*.5f,.73f,-.15f,.04f,.22f,.3f,spot);
            }
            Oval(p,"Bulbo",0,1.25f,-.35f,.95f,.95f,.95f,leaf);
            for(int i=0;i<5;i++)
            {
                float a=i*Mathf.PI*2/5;
                Oval(p,"Hoja del bulbo",Mathf.Cos(a)*.34f,1.24f,-.35f+Mathf.Sin(a)*.34f,.35f,.13f,.83f,leaf,new Vector3(15,i*72,0));
            }
            if(dex>1)
            {
                Oval(p,"Capullo",0,1.8f,-.35f,.65f,.8f,.65f,new Color(.91f,.27f,.44f));
                if(dex==3)
                {
                    Oval(p,"Tronco",0,1.65f,-.35f,.3f,.8f,.3f,new Color(.43f,.3f,.19f));
                    for(int i=0;i<6;i++)
                    { float a=i*Mathf.PI/3; Oval(p,"Pétalo",Mathf.Cos(a)*.5f,2.08f,-.35f+Mathf.Sin(a)*.5f,.65f,.2f,1.1f,new Color(.96f,.36f,.46f),new Vector3(0,-i*60,0)); }
                    Oval(p,"Centro de flor",0,2.22f,-.35f,.5f,.26f,.5f,new Color(1,.78f,.22f));
                }
            }
        }

        static void FireLizard(Parts p,int dex)
        {
            Color skin=p.Main;
            p.Body=Oval(p,"Torso de lagarto",0,.83f,0,.85f,1.22f,.77f,skin);
            Oval(p,"Vientre crema",0,.79f,.325f,.64f,.92f,.15f,Cream);
            p.Head=Oval(p,"Cabeza",0,1.66f,.1f,.85f,.78f,.75f,skin);
            Attach(Oval(p,"Hocico",0,1.48f,.44f,.67f,.32f,.4f,skin),p.Head);
            Face(p,new Vector3(0,1.74f,.44f),.24f,new Color(.13f,.46f,.55f)); Feet(p,skin);
            for(int side=-1;side<=1;side+=2)
                Oval(p,"Brazo",side*.48f,.96f,.06f,.24f,.6f,.26f,skin,new Vector3(0,0,side*25));
            p.Tail=Oval(p,"Cola",0,.5f,-.65f,.33f,.35f,1.25f,skin,new Vector3(-18,0,0));
            p.Flame=Oval(p,"Llama exterior",0,.82f,-1.22f,.37f,.65f,.34f,new Color(1,.23f,.035f));
            Attach(p.Flame,p.Tail);
            Attach(Oval(p,"Corazón de llama",0,.77f,-1.24f,.23f,.43f,.35f,new Color(1,.85f,.12f)),p.Flame);
            if(dex>=5) Attach(Oval(p,"Cresta",0,2.04f,-.04f,.2f,.44f,.25f,skin,new Vector3(-25,0,0)),p.Head);
            if(dex==6)
            {
                for(int side=-1;side<=1;side+=2)
                {
                    var wing=Oval(p,"Ala de Charizard",side*.87f,1.43f,-.39f,1.32f,.9f,.13f,skin,new Vector3(0,side*25,side*22));
                    p.Wings.Add(wing);
                    Attach(Oval(p,"Membrana azul",side*.91f,1.43f,-.3f,1.1f,.69f,.055f,new Color(.1f,.43f,.47f),new Vector3(0,side*25,side*22)),wing);
                    Ear(p,side,new Vector3(side*.26f,2.04f,-.05f),skin,skin,.4f);
                }
            }
        }

        static void Turtle(Parts p,int dex)
        {
            Color shell=new Color(.48f,.25f,.12f), skin=p.Main;
            p.Body=Oval(p,"Cuerpo de tortuga",0,.8f,0,.92f,1.1f,.75f,skin);
            Oval(p,"Borde de caparazón",0,.86f,-.25f,1.12f,1.2f,.62f,Cream);
            Oval(p,"Caparazón marrón",0,.87f,-.42f,.98f,1.07f,.52f,shell);
            Oval(p,"Peto",0,.77f,.3f,.79f,.98f,.23f,Cream);
            for(int i=0;i<3;i++) Oval(p,"Surco del peto",0,.5f+i*.23f,.421f,.64f,.025f,.025f,shell);
            // Raised shell plates remain visible from the following camera.
            for(int side=-1;side<=1;side+=2)
                for(int i=0;i<3;i++) Oval(p,"Placa de caparazón",side*.23f,.55f+i*.28f,-.67f,.38f,.3f,.07f,Color.Lerp(shell,Color.white,.12f));
            p.Head=Oval(p,"Cabeza",0,1.58f,.12f,.94f,.82f,.83f,skin);
            Face(p,new Vector3(0,1.66f,.5f),.25f,new Color(.48f,.17f,.25f)); Feet(p,skin);
            for(int side=-1;side<=1;side+=2) Oval(p,"Brazo",side*.5f,.95f,.15f,.27f,.56f,.3f,skin,new Vector3(0,0,side*35));
            p.Tail=Oval(p,"Cola enrollada",.08f,.47f,-.87f,.48f,.45f,.45f,skin);
            Attach(Oval(p,"Centro del rizo",.08f,.5f,-1.08f,.24f,.23f,.035f,Color.Lerp(skin,Color.black,.25f)),p.Tail);
            if(dex==8) for(int side=-1;side<=1;side+=2) Ear(p,side,new Vector3(side*.43f,1.98f,.04f),skin,Cream,.65f);
            if(dex==9) for(int side=-1;side<=1;side+=2)
            {
                var cannon=p.Add(PrimitiveType.Cylinder,"Cañón",new Vector3(side*.55f,1.43f,-.15f),new Vector3(.3f,.48f,.3f),new Color(.55f,.59f,.63f),new Vector3(65,0,0));
                Attach(Oval(p,"Boca oscura del cañón",side*.55f,1.64f,.29f,.22f,.22f,.04f,Ink,new Vector3(-25,0,0)),cannon);
            }
        }

        static void Fox(Parts p)
        {
            p.Body=Oval(p,"Cuerpo de Eevee",0,.66f,-.1f,.75f,.8f,1.05f,p.Main);
            Feet(p,p.Main,true);
            p.Head=Oval(p,"Cabeza",0,1.24f,.5f,.94f,.82f,.75f,p.Main);
            for(int side=-1;side<=1;side+=2) Ear(p,side,new Vector3(side*.34f,1.82f,.42f),p.Main,new Color(.22f,.12f,.09f),.88f);
            Face(p,new Vector3(0,1.3f,.85f),.23f,new Color(.3f,.13f,.06f));
            Attach(Oval(p,"Hocico",0,1.08f,.87f,.4f,.24f,.22f,Cream),p.Head);
            Attach(Oval(p,"Nariz",0,1.15f,.987f,.09f,.065f,.05f,Ink),p.Head);
            for(int i=0;i<9;i++)
            { float a=i*Mathf.PI*2/9; Oval(p,"Gola de pelo",Mathf.Cos(a)*.34f,.94f+Mathf.Sin(a)*.25f,.2f,.36f,.4f,.38f,Cream); }
            p.Tail=Oval(p,"Cola tupida",0,1.02f,-.86f,.62f,.95f,.6f,p.Main,new Vector3(-40,0,0));
            Attach(Oval(p,"Punta crema",0,1.38f,-1.17f,.48f,.44f,.46f,Cream),p.Tail);
        }

        static void PinkSinger(Parts p)
        {
            p.Body=Oval(p,"Cuerpo redondo",0,.85f,0,1.45f,1.4f,1.15f,p.Main); p.Head=p.Body;
            Face(p,new Vector3(0,.98f,.55f),.31f,new Color(.08f,.58f,.67f));
            for(int side=-1;side<=1;side+=2)
            { Ear(p,side,new Vector3(side*.49f,1.49f,0),p.Main,Ink,.42f); Oval(p,"Brazo",side*.72f,.75f,0,.35f,.25f,.3f,p.Main); }
            Feet(p,p.Main);
            Attach(Oval(p,"Mechón",-.08f,1.48f,.37f,.46f,.49f,.31f,p.Main),p.Head);
            Attach(Oval(p,"Rizo",.06f,1.42f,.54f,.26f,.28f,.06f,Color.Lerp(p.Main,Color.black,.18f)),p.Head);
        }

        static void ShadowCreature(Parts p)
        {
            p.Body=Oval(p,"Cuerpo de Gengar",0,.88f,0,1.4f,1.4f,1.1f,p.Main); p.Head=p.Body;
            Feet(p,p.Main);
            for(int side=-1;side<=1;side+=2)
            { Ear(p,side,new Vector3(side*.48f,1.55f,-.03f),p.Main,p.Main,.65f); Oval(p,"Brazo",side*.72f,.82f,.05f,.5f,.3f,.4f,p.Main); }
            Face(p,new Vector3(0,1.13f,.52f),.31f,new Color(.94f,.08f,.14f));
            Attach(Oval(p,"Sonrisa oscura",0,.73f,.53f,.94f,.4f,.1f,Ink),p.Head);
            for(int i=0;i<7;i++) Attach(Oval(p,"Diente",-.36f+i*.12f,.73f,.59f,.105f,.24f,.035f,Cream),p.Head);
            for(int i=0;i<5;i++) Oval(p,"Púa dorsal",-.44f+i*.22f,1.35f,-.42f,.23f,.55f,.24f,p.Main,new Vector3(-30,0,(2-i)*18));
        }

        static void Sleeper(Parts p)
        {
            Color blue=p.Main;
            p.Body=Oval(p,"Barriga voluminosa",0,.96f,0,1.7f,1.75f,1.25f,blue);
            Oval(p,"Vientre crema",0,.92f,.5f,1.35f,1.42f,.3f,Cream);
            p.Head=Oval(p,"Cabeza",0,1.92f,.05f,1.23f,.87f,.95f,blue);
            Attach(Oval(p,"Cara crema",0,1.86f,.44f,1.02f,.57f,.18f,Cream),p.Head);
            for(int side=-1;side<=1;side+=2)
            {
                Ear(p,side,new Vector3(side*.46f,2.31f,.02f),blue,blue,.4f);
                Attach(Oval(p,"Ojo cerrado",side*.23f,1.97f,.539f,.22f,.035f,.025f,Ink),p.Head);
                Oval(p,"Brazo",side*.83f,1.03f,.03f,.5f,.95f,.52f,blue,new Vector3(0,0,side*25));
            }
            Attach(Oval(p,"Boca",0,1.75f,.55f,.35f,.035f,.025f,Ink),p.Head);
            Feet(p,Cream);
        }
    }
}

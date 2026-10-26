from pathlib import Path
import shutil

from pymeshio.pmx.reader import read_from_file


SOURCE = Path(r"C:\Users\andres\AppData\Local\Temp\opencode\Trainer\Trainer")
DESTINATION = Path(r"C:\Users\andres\Documents\PokeMoba\UnityProject\Assets\_Project\Resources\Models\Trainers\Imported")


def color(value):
    return "{:.6f} {:.6f} {:.6f}".format(float(value[0]), float(value[1]), float(value[2]))


def convert(source_path: Path):
    model = read_from_file(str(source_path))
    target = DESTINATION / source_path.parent.name
    target.mkdir(parents=True, exist_ok=True)

    obj_path = target / (source_path.stem + ".obj")
    mtl_path = target / (source_path.stem + ".mtl")
    vertices = model.vertices

    with obj_path.open("w", encoding="utf-8") as obj:
        obj.write("# Converted from PMX for PokeMoba\n")
        obj.write(f"mtllib {mtl_path.name}\n")
        for vertex in vertices:
            x, y, z = vertex.position
            obj.write(f"v {x:.7f} {y:.7f} {z:.7f}\n")
        for vertex in vertices:
            nx, ny, nz = vertex.normal
            obj.write(f"vn {nx:.7f} {ny:.7f} {nz:.7f}\n")
        for vertex in vertices:
            u, v = vertex.uv
            obj.write(f"vt {u:.7f} {1.0 - v:.7f}\n")

        cursor = 0
        for material_index, material in enumerate(model.materials):
            obj.write(f"usemtl mat_{material_index}\n")
            for _ in range(material.vertex_count // 3):
                a, b, c = model.indices[cursor:cursor + 3]
                cursor += 3
                obj.write(f"f {a + 1}/{a + 1}/{a + 1} {b + 1}/{b + 1}/{b + 1} {c + 1}/{c + 1}/{c + 1}\n")

    with mtl_path.open("w", encoding="utf-8") as mtl:
        for material_index, material in enumerate(model.materials):
            mtl.write(f"newmtl mat_{material_index}\n")
            mtl.write(f"Kd {color(material.diffuse_color)}\n")
            mtl.write(f"d {float(material.alpha):.6f}\n")
            if 0 <= material.texture_index < len(model.textures):
                texture = Path(model.textures[material.texture_index].replace("\\", "/"))
                source_texture = source_path.parent / texture
                if source_texture.exists():
                    shutil.copy2(source_texture, target / texture.name)
                    mtl.write(f"map_Kd {texture.name}\n")
            mtl.write("\n")


def main():
    for model_path in sorted(SOURCE.glob("**/*.pmx")):
        convert(model_path)
        print(model_path.stem)


if __name__ == "__main__":
    main()

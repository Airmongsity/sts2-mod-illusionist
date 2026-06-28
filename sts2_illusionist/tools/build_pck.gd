extends SceneTree

# Packs all mod resources under res://illusionist/ into illusionist.pck.
# The game expects modded loc tables at res://<mod_id>/localization/<lang>/<file>,
# so the in-pck paths must mirror the project layout exactly.

const OUTPUT_DIR := "res://build"
const OUTPUT_FILE := "res://build/illusionist.pck"
const CONTENT_DIRS := ["res://illusionist"]
const SKIP_EXTENSIONS := [".import", ".uid"]

# Textures that must be shipped as IMPORTED Godot resources (.ctex) rather than raw bytes, because
# something loads them through ResourceLoader instead of FileAccess. spine-godot's atlas loader is the
# case in point: it does ResourceLoader.load() on the atlas's texture, which only works if the PCK
# contains the .import sidecar + the compiled .ctex (the raw source PNG is packed by no one). The build
# script runs a `--import` pass first so these .ctex files exist under res://.godot/imported/.
const IMPORTED_TEXTURES := [
	"res://illusionist/art/illusionist_energy_icon.webp", # in-text energy icon ([img] in descriptions)
]

# Spine atlases whose texture PAGES must all be imported (spine-godot loads them via ResourceLoader).
# Every page filename listed inside each .atlas is imported automatically, so re-exporting with a
# different number of pages (skeleton.png, skeleton2.png, skeleton3.png, …) just works.
const SPINE_ATLASES := [
	"res://illusionist/art/skeleton.atlas",     # combat flipbook
	"res://illusionist/art/illusionist.atlas",  # rest-site single image
]

func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute(OUTPUT_DIR)
	var packer := PCKPacker.new()
	var ok := packer.pck_start(OUTPUT_FILE)
	if ok != OK:
		push_error("pck_start failed: %s" % ok)
		quit(1)
		return

	var added := 0
	for dir in CONTENT_DIRS:
		added += _add_dir_recursive(packer, dir)

	# Pack the imported-texture chain (.import sidecar + compiled .ctex) for each special texture.
	for tex in IMPORTED_TEXTURES:
		added += _add_imported_texture(packer, tex)

	# Pack every texture page referenced by each spine atlas (auto-discovered from the .atlas text).
	for atlas in SPINE_ATLASES:
		for page in _atlas_pages(atlas):
			added += _add_imported_texture(packer, page)

	if added == 0:
		push_error("No files were added to the PCK. Check CONTENT_DIRS paths.")
		quit(1)
		return

	var flush_ok := packer.flush()
	if flush_ok != OK:
		push_error("flush failed: %s" % flush_ok)
		quit(1)
		return

	print("PCK built: %s (%d files)" % [OUTPUT_FILE, added])
	quit(0)

func _add_dir_recursive(packer: PCKPacker, dir_path: String) -> int:
	var count := 0
	var dir := DirAccess.open(dir_path)
	if dir == null:
		push_warning("Could not open dir: %s" % dir_path)
		return 0
	dir.list_dir_begin()
	var name := dir.get_next()
	while name != "":
		if name == "." or name == "..":
			name = dir.get_next()
			continue
		var full := dir_path.path_join(name)
		if dir.current_is_dir():
			count += _add_dir_recursive(packer, full)
		else:
			var skip := false
			for ext in SKIP_EXTENSIONS:
				if full.ends_with(ext):
					skip = true
					break
			# Raw sources of imported textures are shipped as .ctex instead — don't pack the raw file.
			if full in IMPORTED_TEXTURES:
				skip = true
			if not skip:
				var add_ok := packer.add_file(full, full)
				if add_ok != OK:
					push_error("add_file failed: %s %s" % [full, add_ok])
					quit(1)
				else:
					count += 1
		name = dir.get_next()
	dir.list_dir_end()
	return count

# Packs <texture>.import plus every compiled resource it references under res://.godot/imported/.
func _add_imported_texture(packer: PCKPacker, tex_path: String) -> int:
	var import_path := tex_path + ".import"
	if not FileAccess.file_exists(import_path):
		# Art is gitignored; a code-only checkout won't have it. Skip gracefully (the author's full
		# checkout has the files, so their packaged PCK includes them).
		push_warning("Imported texture missing, skipping: %s" % tex_path)
		return 0

	var count := 0
	# 1) the .import sidecar itself
	if packer.add_file(import_path, import_path) == OK:
		count += 1
	else:
		push_error("add_file failed: %s" % import_path)
		quit(1)

	# 2) every res://.godot/imported/*.ctex the sidecar points at
	var text := FileAccess.get_file_as_string(import_path)
	for ctex in _extract_imported_resources(text):
		if not FileAccess.file_exists(ctex):
			push_error("Compiled resource missing (re-run --import): %s" % ctex)
			quit(1)
			return count
		if packer.add_file(ctex, ctex) == OK:
			count += 1
		else:
			push_error("add_file failed: %s" % ctex)
			quit(1)
	return count

# Texture-page res:// paths referenced by a spine .atlas (the lines that are bare image filenames).
func _atlas_pages(atlas_path: String) -> Array:
	var pages := []
	if not FileAccess.file_exists(atlas_path):
		push_warning("Atlas missing, skipping pages: %s" % atlas_path)
		return pages
	var dir := atlas_path.get_base_dir()
	for raw in FileAccess.get_file_as_string(atlas_path).split("\n"):
		var line := raw.strip_edges()
		var low := line.to_lower()
		if low.ends_with(".png") or low.ends_with(".webp"):
			pages.append(dir.path_join(line))
	return pages

# Pull unique "res://.godot/imported/....<ext>" tokens out of an .import file's text.
func _extract_imported_resources(text: String) -> Array:
	var found := {}
	var needle := "res://.godot/imported/"
	var from := text.find(needle)
	while from != -1:
		var end := from
		# resource paths are quoted strings; read until the closing quote
		while end < text.length() and text[end] != '"':
			end += 1
		var token := text.substr(from, end - from)
		found[token] = true
		from = text.find(needle, end)
	return found.keys()

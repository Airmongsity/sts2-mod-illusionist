shader_type canvas_item;

// Multiply-tint for the Illusionist card frame. Recolours the base frame texture toward a flat colour
// while keeping its tonal detail (highlights/shadows): the lightest parts read as `tint`, darker parts
// stay proportionally darker. Set `tint` from code (IllusionistFramePatch) to the desired frame colour.
uniform vec4 tint : source_color = vec4(1.0, 1.0, 1.0, 1.0);

void fragment() {
    vec4 t = texture(TEXTURE, UV);
    COLOR = vec4(t.rgb * tint.rgb, t.a);
}

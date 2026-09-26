"""Encode actual Unity Play Mode PNG frames. This does not render Blender geometry."""
import bpy
from pathlib import Path
root=Path(__file__).resolve().parents[3]
frames=root/'output/visual-upgrade/after/video-frames'
files=sorted(frames.glob('frame-*.png'))
if not files: raise RuntimeError('Capture Unity Play Mode frames before encoding.')
scene=bpy.context.scene
scene.sequence_editor_create()
strip=scene.sequence_editor.strips.new_image('Unity Play Mode capture',str(files[0]),channel=1,frame_start=1)
for path in files[1:]: strip.elements.append(path.name)
strip.frame_final_duration=len(files)
scene.render.resolution_x=1280
scene.render.resolution_y=720
scene.render.resolution_percentage=100
scene.render.fps=12
scene.frame_start=1
scene.frame_end=len(files)
scene.render.image_settings.media_type='VIDEO'
scene.render.image_settings.file_format='FFMPEG'
scene.view_settings.view_transform='Standard'
scene.view_settings.exposure=0
scene.view_settings.gamma=1
scene.render.ffmpeg.format='MPEG4'
scene.render.ffmpeg.codec='H264'
scene.render.ffmpeg.constant_rate_factor='HIGH'
scene.render.filepath=str(root/'output/visual-upgrade/unity-reaction-review.mp4')
scene.render.use_sequencer=True
bpy.ops.render.render(animation=True)
print('UNITY_CAPTURE_VIDEO_ENCODED',len(files),scene.render.filepath)

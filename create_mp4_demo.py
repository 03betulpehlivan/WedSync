
import os
import time
from moviepy.editor import ImageSequenceClip
from PIL import Image

def create_mp4_demo():
    source_dir = r"C:\Users\ENES\Desktop\SaaS\dugunsalonu\wwwroot\demo" # Place to save
    if not os.path.exists(source_dir):
        os.makedirs(source_dir)
        
    temp_dir = r"C:\Users\ENES\.gemini\antigravity\brain\d9a590c9-f2bf-4d03-8980-5639eaba8642\.tempmediaStorage"
    output_path = os.path.join(source_dir, "mobil_deneyim_video.mp4")
    
    # Get files from temp dir
    all_files = os.listdir(temp_dir)
    png_files = [f for f in all_files if f.endswith('.png') and f.startswith('media_d9a590c9')]
    
    # Sort by timestamp
    def get_ts(filename):
        try:
            return int(filename.split('_')[-1].split('.')[0])
        except:
            return 0
            
    png_files.sort(key=get_ts)
    
    # Filter for the last session (roughly last 15 mins)
    current_ts = int(time.time() * 1000)
    recent_files = [f for f in png_files if get_ts(f) > (current_ts - 15 * 60 * 1000)]
    
    if not recent_files:
        print("No recent screenshots found.")
        # Fallback to all pngs if none in last 15 mins (for safety)
        recent_files = png_files[-20:] # Take last 20

    print(f"Processing {len(recent_files)} frames...")
    
    image_paths = [os.path.join(temp_dir, f) for f in recent_files]
    
    # Create clip
    # fps=1 for 1 second per image, or higher for smoother video. 
    # Since these are discrete steps, maybe 0.5 fps (2 seconds per step) is better.
    clip = ImageSequenceClip(image_paths, fps=0.5)
    
    # Write to file
    clip.write_videofile(output_path, codec="libx264")
    print(f"Video created at: {output_path}")

if __name__ == "__main__":
    create_mp4_demo()

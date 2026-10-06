
import os
from PIL import Image

def create_video():
    source_dir = r"C:\Users\ENES\.gemini\antigravity\brain\00ef8dcd-69c6-4362-9079-65845ed057c9\.tempmediaStorage"
    output_path = r"c:\Users\ENES\Desktop\SaaS\dugunsalonu\wwwroot\demo\misafir_deneyimi_video.webp"
    list_file = r"c:\Users\ENES\Desktop\SaaS\dugunsalonu\screenshots_list_guest.txt"
    
    # Başlangıç zaman damgası (Misafir deneyimi denemesi)
    start_ts = 1778071541714
    
    with open(list_file, 'r', encoding='utf-8') as f:
        all_files = [line.strip().replace('\ufeff', '') for line in f if line.strip()]
    
    demo_files = []
    for f in all_files:
        if not f.endswith('.png'): continue
        try:
            ts_str = f.split('_')[-1].split('.')[0]
            ts = int(ts_str)
            if ts >= start_ts:
                demo_files.append((ts, f))
        except:
            continue
            
    demo_files.sort()
    
    if not demo_files:
        print("Hata: Görüntü bulunamadı.")
        return

    print(f"{len(demo_files)} kare bulundu. İşleniyor...")

    frames = []
    for ts, f_path in demo_files:
        img = Image.open(f_path)
        img.thumbnail((800, 1600)) # Mobil format için dikey öncelik
        frames.append(img)
    
    frames[0].save(
        output_path,
        save_all=True,
        append_images=frames[1:],
        duration=1500, # Yavaş ve anlaşılır (1.5 saniye)
        loop=0,
        quality=90
    )
    print(f"Video başarıyla oluşturuldu: {output_path}")

if __name__ == "__main__":
    create_video()

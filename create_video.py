
import os
from PIL import Image

def create_video():
    source_dir = r"C:\Users\ENES\.gemini\antigravity\brain\00ef8dcd-69c6-4362-9079-65845ed057c9\.tempmediaStorage"
    output_path = r"c:\Users\ENES\Desktop\SaaS\dugunsalonu\wwwroot\demo\gercek_demo_video.webp"
    list_file = r"c:\Users\ENES\Desktop\SaaS\dugunsalonu\screenshots_list.txt"
    
    # Başlangıç zaman damgası (son başarılı deneme)
    start_ts = 1778070754237
    
    with open(list_file, 'r', encoding='utf-8') as f:
        all_files = [line.strip().replace('\ufeff', '') for line in f if line.strip()]
    
    # Sadece son denemeye ait ve PNG olan dosyaları al
    demo_files = []
    for f in all_files:
        if not f.endswith('.png'): continue
        try:
            # Örn: media_..._1778070754237.png
            ts_str = f.split('_')[-1].split('.')[0]
            ts = int(ts_str)
            if ts >= start_ts:
                demo_files.append((ts, f))
        except:
            continue
            
    # Zaman sırasına göre diz
    demo_files.sort()
    
    if not demo_files:
        print("Hata: Görüntü bulunamadı.")
        return

    print(f"{len(demo_files)} kare bulundu. İşleniyor...")

    frames = []
    for ts, f_path in demo_files:
        img = Image.open(f_path)
        # Boyutu biraz küçültelim ki dosya çok devasa olmasın
        img.thumbnail((1280, 720))
        frames.append(img)
    
    # İlk kareyi baz alarak animasyon oluştur
    frames[0].save(
        output_path,
        save_all=True,
        append_images=frames[1:],
        duration=1500, # Her kare 1.5 saniye (Okunabilirlik için yavaşlatıldı)
        loop=0,
        quality=90
    )
    print(f"Video başarıyla oluşturuldu: {output_path}")

if __name__ == "__main__":
    create_video()

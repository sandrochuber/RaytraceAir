ffmpeg -f image2 -i frame_%04d.jpg -c:v h264 -r 24 -b:v 20M Capture.mkv
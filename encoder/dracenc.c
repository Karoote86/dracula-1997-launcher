/* dracenc - minimal Ogg Vorbis encoder for raw CD audio (44.1 kHz, 16-bit, stereo, little endian).
 * Usage: dracenc <in.pcm> <out.ogg> [quality -0.1..1.0, default 0.7]
 * Written for the Dracula launcher; uses libogg + libvorbis (BSD licence, see third_party).
 * The encode loop follows libvorbis' examples/encoder_example.c.
 */
#include <stdio.h>
#include <stdlib.h>
#include <time.h>
#include <vorbis/vorbisenc.h>

#define READ 1024

int main(int argc, char **argv)
{
    if (argc < 3) { fprintf(stderr, "usage: dracenc in.pcm out.ogg [quality]\n"); return 2; }
    float q = argc > 3 ? (float)atof(argv[3]) : 0.7f;
    FILE *in = fopen(argv[1], "rb");
    if (!in) { perror(argv[1]); return 1; }
    FILE *out = fopen(argv[2], "wb");
    if (!out) { perror(argv[2]); return 1; }

    ogg_stream_state os; ogg_page og; ogg_packet op;
    vorbis_info vi; vorbis_comment vc; vorbis_dsp_state vd; vorbis_block vb;
    static signed char buf[READ * 4];

    vorbis_info_init(&vi);
    if (vorbis_encode_init_vbr(&vi, 2, 44100, q)) { fprintf(stderr, "bad quality\n"); return 1; }
    vorbis_comment_init(&vc);
    vorbis_comment_add_tag(&vc, "ENCODER", "dracenc (libvorbis)");
    vorbis_analysis_init(&vd, &vi);
    vorbis_block_init(&vd, &vb);
    srand((unsigned)time(NULL));
    ogg_stream_init(&os, rand());

    {
        ogg_packet h, hc, hcode;
        vorbis_analysis_headerout(&vd, &vc, &h, &hc, &hcode);
        ogg_stream_packetin(&os, &h);
        ogg_stream_packetin(&os, &hc);
        ogg_stream_packetin(&os, &hcode);
        while (ogg_stream_flush(&os, &og)) {
            fwrite(og.header, 1, og.header_len, out);
            fwrite(og.body, 1, og.body_len, out);
        }
    }

    int eos = 0;
    while (!eos) {
        long bytes = (long)fread(buf, 1, READ * 4, in);
        if (bytes < 4) {
            vorbis_analysis_wrote(&vd, 0);
        } else {
            long n = bytes / 4, i;
            float **b = vorbis_analysis_buffer(&vd, READ);
            for (i = 0; i < n; i++) {
                b[0][i] = ((buf[i * 4 + 1] << 8) | (0x00ff & (int)buf[i * 4])) / 32768.f;
                b[1][i] = ((buf[i * 4 + 3] << 8) | (0x00ff & (int)buf[i * 4 + 2])) / 32768.f;
            }
            vorbis_analysis_wrote(&vd, n);
        }
        while (vorbis_analysis_blockout(&vd, &vb) == 1) {
            vorbis_analysis(&vb, NULL);
            vorbis_bitrate_addblock(&vb);
            while (vorbis_bitrate_flushpacket(&vd, &op)) {
                ogg_stream_packetin(&os, &op);
                while (!eos) {
                    if (!ogg_stream_pageout(&os, &og)) break;
                    fwrite(og.header, 1, og.header_len, out);
                    fwrite(og.body, 1, og.body_len, out);
                    if (ogg_page_eos(&og)) eos = 1;
                }
            }
        }
    }

    ogg_stream_clear(&os);
    vorbis_block_clear(&vb);
    vorbis_dsp_clear(&vd);
    vorbis_comment_clear(&vc);
    vorbis_info_clear(&vi);
    fclose(in);
    if (fclose(out)) { perror(argv[2]); return 1; }
    return 0;
}


import sys

def count_braces(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    depth = 0
    for i, line in enumerate(lines):
        line_num = i + 1
        for char in line:
            if char == '{':
                depth += 1
                # print(f"L{line_num}: {{ -> depth {depth}")
            elif char == '}':
                depth -= 1
                # print(f"L{line_num}: }} -> depth {depth}")
                if depth < 0:
                    print(f"Error: Llave de cierre extra en la línea {line_num}")
                    return
        
        # Si queremos ver el estado en puntos críticos
        if line_num in [114, 156, 187, 188, 189, 290]:
            print(f"Línea {line_num}: Profundidad de llaves = {depth}")

if __name__ == "__main__":
    count_braces(sys.argv[1])

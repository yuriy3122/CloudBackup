export type EnumValueType = string | number;

export type EnumType<TKey extends string, TValue extends EnumValueType> = {
  [TP in TKey]: TValue
};

// let key: keyof typeof filter;
//<TValue extends EnumValueType, T extends EnumType<keyof T, TValue>>

export class EnumValues {
  static getNamesAndValues<T>(enumObject: T) {
    return this.getNames(enumObject).map(name => { return { name: name, value: enumObject[name] }; });
  }

  static getNames<T>(enumObject: T) {
    return Object.keys(enumObject).filter(key => isNaN(+key)) as (keyof T)[];
  }

  static getNameFromValue<T>(e: T, value: T[keyof T]) {
    const all = this.getNamesAndValues(e).filter(pair => pair.value === value);
    return all.length === 1 ? all[0].name : null;
  }

  static getValues<T>(e: T) {
    return this.getNames(e).map(name => e[name]);
  }
}
